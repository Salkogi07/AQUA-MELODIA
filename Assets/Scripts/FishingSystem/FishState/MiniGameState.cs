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

            // 마우스 휠로 내 위치(Reel Ratio) 조절 (0.0 ~ 1.0)
            if (scrollDelta != 0)
            {
                fishingRod.PlayerReelRatio.Value += scrollDelta * fishingRod.wheelSensitivity;
                fishingRod.PlayerReelRatio.Value = Mathf.Clamp01(fishingRod.PlayerReelRatio.Value);
            }
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
            //fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Slack);
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