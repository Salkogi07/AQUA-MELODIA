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
        
        private Vector3 landedBobberPosition;    
        private float centerTransitionProgress = 0f; 
        private const float transitionDuration = 1.0f; 

        private float currentReelingAnimValue = 0f;
        private float reelingTimer = 0f; 
        private float reelingTimeout = 0.15f; 

        // [개선] 룰(HandleRules)과 무브먼트 스레드 간의 속도 비율 동기화를 위한 클래스 필드화
        private float speedMultiplier = 1f;
        
        public MiniGameState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            cts = new CancellationTokenSource();

            hookedFish = fishingRod.CurrentHookedFish;
            currentFishPositionX = 0f; 
            
            landedBobberPosition = fishingRod.bobber.position;
            initialBobberPosition = landedBobberPosition; 
            centerTransitionProgress = 0f;

            fishingRod.ResetBobberPhysics();
            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Taut);

            fishingRod.PlayerReelRatio.Value = 0.5f;
            fishingRod.LineStress.Value = 0f;
            fishingRod.FishHpRatio.Value = 1f;
            fishingRod.IsMiniGameActive.Value = true;
            
            currentReelingAnimValue = 0f;
            reelingTimer = 0f;
            anim.SetFloat("reeling", 0f);

            // 1. 민첩 및 상쇄 비율을 미니게임 진입 시점에 선 연산 및 필드 저장
            float fishAgility = hookedFish.Data.agility > 0f ? hookedFish.Data.agility : 0f;
            float rodAgility = fishingRod.EffectiveRodAgility > 0f ? fishingRod.EffectiveRodAgility : 0f;
            float remainingAgility = Mathf.Max(0f, fishAgility - rodAgility);

            // 민첩 상쇄 차감 공식 적용 (상쇄 실패한 만큼 speedMultiplier가 1.0~5.0배 사이로 가산)
            speedMultiplier = Mathf.Clamp(1f + (remainingAgility * 0.2f), 1.0f, 5.0f);

            FishMovementRoutineAsync(cts.Token).Forget();
        }

        public override void Update()
        {
            if (centerTransitionProgress < 1f)
            {
                centerTransitionProgress += Time.deltaTime / transitionDuration;
                if (centerTransitionProgress > 1f) centerTransitionProgress = 1f;
            }

            HandleInput();
            HandleRules();
            UpdateVisuals();
        }

        private void HandleInput()
        {
            float scrollDelta = Input.mouseScrollDelta.y;

            if (scrollDelta != 0)
            {
                fishingRod.PlayerReelRatio.Value += scrollDelta * fishingRod.wheelSensitivity;
                fishingRod.PlayerReelRatio.Value = Mathf.Clamp01(fishingRod.PlayerReelRatio.Value);
                reelingTimer = reelingTimeout;
            }

            if (reelingTimer > 0f)
            {
                reelingTimer -= Time.deltaTime;
            }

            float targetReelingValue = (reelingTimer > 0f) ? 1f : 0f;
            currentReelingAnimValue = Mathf.Lerp(currentReelingAnimValue, targetReelingValue, Time.deltaTime * 15f);
            anim.SetFloat("reeling", currentReelingAnimValue);
        }
        
        private void HandleRules()
        {
            float mappedFishRatio = Mathf.InverseLerp(fishingRod.patternMinX, fishingRod.patternMaxX, currentFishPositionX);
            fishingRod.FishUiRatio.Value = mappedFishRatio;

            // 물고기가 날뛰는 속도(speedMultiplier)가 빠를수록 조준 영역(Sweet Spot) 크기를 동적으로 대폭 축소합니다.
            // 속도가 빨라질수록 맞추어야 할 범위가 매우 정밀해지므로, 정적인 방치 플레이가 원천적으로 불가능해집니다.
            float dynamicTolerance = fishingRod.EffectiveSweetSpotTolerance / speedMultiplier;

            float difference = Mathf.Abs(fishingRod.PlayerReelRatio.Value - fishingRod.FishUiRatio.Value);
            bool isInsideSweetSpot = difference <= dynamicTolerance;

            if (isInsideSweetSpot)
            {
                // 물고기 힘(Strength) 과 낚싯대 힘(EffectiveRodPower)의 비율 계산
                float fishStrength = hookedFish.Data.strength > 0f ? hookedFish.Data.strength : 1f;
                float rodPower = fishingRod.EffectiveRodPower;
                
                float powerMultiplier = rodPower / fishStrength;
                powerMultiplier = Mathf.Clamp(powerMultiplier, 0.25f, 3.0f); 

                float finalStaminaDamage = fishingRod.EffectiveDamageRate * powerMultiplier;
                hookedFish.CurrentStamina -= finalStaminaDamage * Time.deltaTime;
                
                fishingRod.LineStress.Value -= fishingRod.stressDecreaseRate * Time.deltaTime;
            }
            else
            {
                // 물고기가 날뛰는 속도가 빠를수록, 조준 범위를 벗어났을 때 누적되는 낚싯줄 텐션 스트레스가 배율로 증가합니다.
                // 휠을 조작하지 않고 방치할 시 아주 빠른 속도(약 2~3초 내외)로 줄이 끊어지게 유도합니다.
                float dynamicStressIncrease = fishingRod.stressIncreaseRate * speedMultiplier;
                fishingRod.LineStress.Value += dynamicStressIncrease * Time.deltaTime;
            }

            fishingRod.LineStress.Value = Mathf.Clamp01(fishingRod.LineStress.Value);
            fishingRod.FishHpRatio.Value = hookedFish.CurrentStamina / hookedFish.Data.maxStamina;

            if (hookedFish.CurrentStamina <= 0)
            {
                cts?.Cancel();
                stateMachine.ChangeState(fishingRod.FinalStruggleState);
                return;
            }

            if (fishingRod.LineStress.Value >= 1f)
            {
                FailMiniGame("💥 물고기가 거칠게 요동쳐 낚싯줄이 압력을 견디지 못하고 끊어졌습니다!");
                return;
            }
        }

        private void UpdateVisuals()
        {
            float currentBaselineX = Mathf.Lerp(landedBobberPosition.x, fishingRod.currentZoneCenterX, centerTransitionProgress);

            Vector3 targetPos = initialBobberPosition;
            targetPos.x = currentBaselineX + currentFishPositionX; 
            fishingRod.bobber.position = targetPos;
        }

        private async UniTaskVoid FishMovementRoutineAsync(CancellationToken token)
        {
            PatternDataSO pattern = hookedFish.Data.patternData;
            if (pattern == null || pattern.patternNodes.Count == 0) return; 

            int patternIndex = 0;

            try
            {
                while (hookedFish.CurrentStamina > 0)
                {
                    PatternNode currentNode = pattern.patternNodes[patternIndex];
                    float targetX = currentNode.targetPositionX;
                    float waitTime = currentNode.waitTime;
                    float duration = currentNode.moveDuration;

                    float startX = currentFishPositionX;
                    float timeElapsed = 0f;

                    // 미니게임 시작 시 캐싱해 둔 상쇄 속도 비율(speedMultiplier) 적용
                    float effectiveDuration = duration / speedMultiplier;

                    if (effectiveDuration > 0f)
                    {
                        while (timeElapsed < effectiveDuration)
                        {
                            timeElapsed += Time.deltaTime;
                            float t = Mathf.Clamp01(timeElapsed / effectiveDuration);
                            t = t * t * (3f - 2f * t);

                            currentFishPositionX = Mathf.Lerp(startX, targetX, t);
                            await UniTask.Yield(PlayerLoopTiming.Update, token);
                        }
                    }
                    
                    currentFishPositionX = targetX;

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