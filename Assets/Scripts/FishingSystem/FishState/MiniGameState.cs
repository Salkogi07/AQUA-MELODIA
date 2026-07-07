using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fish;
using FishingSystem.Fishing_Pattern;

namespace FishingSystem.FishState
{
    public class MiniGameState : FishingState
    {
        private CancellationTokenSource cts;
        
        private FishData hookedFish;
        private float currentFishPositionX; 
        private Vector3 initialBobberPosition;
        
        // 릴링 애니메이션 제어용 변수
        private float currentReelingAnimValue = 0f;
        private float reelingTimer = 0f; // 휠 입력 유지 타이머
        private float reelingTimeout = 0.15f; // 휠을 굴린 후 모션이 유지되는 시간 (초)
        
        public MiniGameState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            cts = new CancellationTokenSource();

            hookedFish = fishingRod.CurrentHookedFish;
            currentFishPositionX = 0f; 
            initialBobberPosition = fishingRod.bobber.position; 

            fishingRod.ResetBobberPhysics();
            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Taut);

            // R3 변수들 초기화 (중앙 0.5부터 시작)
            fishingRod.PlayerReelRatio.Value = 0.5f;
            fishingRod.LineStress.Value = 0f;
            fishingRod.FishHpRatio.Value = 1f;
            fishingRod.IsMiniGameActive.Value = true;
            
            // 애니메이션 변수 초기화
            currentReelingAnimValue = 0f;
            reelingTimer = 0f;
            anim.SetFloat("reeling", 0f);

            FishMovementRoutineAsync(cts.Token).Forget();
        }

        public override void Update()
        {
            HandleInput();
            HandleRules();
            UpdateVisuals();
        }

        private void HandleInput()
        {
            float scrollDelta = Input.mouseScrollDelta.y;

            // 1. 마우스 휠 조작이 들어온 프레임
            if (scrollDelta != 0)
            {
                // UI 게이지 이동
                fishingRod.PlayerReelRatio.Value += scrollDelta * fishingRod.wheelSensitivity;
                fishingRod.PlayerReelRatio.Value = Mathf.Clamp01(fishingRod.PlayerReelRatio.Value);

                // 💡 휠을 굴렸으므로 타이머를 가득 채움
                reelingTimer = reelingTimeout;
            }

            // 2. 타이머 차감
            if (reelingTimer > 0f)
            {
                reelingTimer -= Time.deltaTime;
            }

            // 3. 애니메이션 트리 제어 (타이머가 남아있으면 1, 아니면 0으로 목표 설정)
            float targetReelingValue = (reelingTimer > 0f) ? 1f : 0f;

            // Lerp로 값을 부드럽게 전환 (15f는 반응 속도, 값이 클수록 빠르게 전환됨)
            currentReelingAnimValue = Mathf.Lerp(currentReelingAnimValue, targetReelingValue, Time.deltaTime * 15f);
            
            // Animator에 적용
            anim.SetFloat("reeling", currentReelingAnimValue);
        }
        
        private void HandleRules()
        {
            // 1. 물고기의 월드 X좌표를 UI 스케일(0~1)로 변환
            float mappedFishRatio = Mathf.InverseLerp(fishingRod.patternMinX, fishingRod.patternMaxX, currentFishPositionX);
            fishingRod.FishUiRatio.Value = mappedFishRatio;

            // 2. 오차범위(10% = 0.1f) 내에 있는지 확인
            float difference = Mathf.Abs(fishingRod.PlayerReelRatio.Value - fishingRod.FishUiRatio.Value);
            bool isInsideSweetSpot = difference <= fishingRod.sweetSpotTolerance;

            if (isInsideSweetSpot)
            {
                // 적중! 물고기 체력 깎고 스트레스 감소
                hookedFish.CurrentStamina -= fishingRod.fishDamageRate * Time.deltaTime;
                fishingRod.LineStress.Value -= fishingRod.stressDecreaseRate * Time.deltaTime;
            }
            else
            {
                // 빗나감! 줄 스트레스(위험도) 증가
                fishingRod.LineStress.Value += fishingRod.stressIncreaseRate * Time.deltaTime;
            }

            // 0 ~ 1.0으로 고정
            fishingRod.LineStress.Value = Mathf.Clamp01(fishingRod.LineStress.Value);
            fishingRod.FishHpRatio.Value = hookedFish.CurrentStamina / hookedFish.Data.maxStamina;

            // 3. 승패 체크
            if (hookedFish.CurrentStamina <= 0)
            {
                cts?.Cancel();
                fishingRod.StartFinalStrugglePattern();
                return;
            }

            if (fishingRod.LineStress.Value >= 1f) // 스트레스가 끝까지 차면
            {
                FailMiniGame("💥 물고기를 놓쳐 줄이 끊어졌습니다!");
                return;
            }
        }

        private void UpdateVisuals()
        {
            // 실제 월드 찌(Bobber) 움직임 연출
            Vector3 targetPos = initialBobberPosition;
            targetPos.x += currentFishPositionX; 
            fishingRod.bobber.position = targetPos;
        }

        private async UniTaskVoid FishMovementRoutineAsync(CancellationToken token)
        {
            PatternDataSO pattern = hookedFish.Data.patternData;
            if (pattern == null || pattern.patternNodes.Count == 0) return; 

            float moveSpeed = hookedFish.Data.agility;
            int patternIndex = 0;

            try
            {
                while (hookedFish.CurrentStamina > 0)
                {
                    PatternNode currentNode = pattern.patternNodes[patternIndex];
                    float targetX = currentNode.targetPositionX;
                    float waitTime = currentNode.waitTime;

                    while (Mathf.Abs(currentFishPositionX - targetX) > 0.05f)
                    {
                        currentFishPositionX = Mathf.Lerp(currentFishPositionX, targetX, Time.deltaTime * moveSpeed);
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }

                    if (waitTime > 0f)
                    {
                        await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: token);
                    }

                    patternIndex++;
                    if (patternIndex >= pattern.patternNodes.Count)
                    {
                        if (pattern.loopPattern) patternIndex = 0; 
                        else break; 
                    }
                }
            }
            catch (System.OperationCanceledException) { }
        }

        private void FailMiniGame(string reasonMsg)
        {
            Debug.Log($"<color=#FF5555>{reasonMsg}</color>");
            cts?.Cancel();
            fishingRod.CurrentHookedFish = null;
            stateMachine.ChangeState(fishingRod.FailedState);
        }

        public override void Exit()
        {
            base.Exit();
            cts?.Cancel();
            cts?.Dispose();
            fishingRod.IsMiniGameActive.Value = false; 
        }
    }
}