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

            float difference = Mathf.Abs(fishingRod.PlayerReelRatio.Value - fishingRod.FishUiRatio.Value);
            bool isInsideSweetSpot = difference <= fishingRod.EffectiveSweetSpotTolerance;

            if (isInsideSweetSpot)
            {
                // 물고기 힘(Strength) 과 낚싯대 힘(EffectiveRodPower)의 비율 계산
                float fishStrength = hookedFish.Data.strength > 0f ? hookedFish.Data.strength : 1f;
                float rodPower = fishingRod.EffectiveRodPower;
                
                // 아무리 장비 차이가 극심해도 '최소 0.25배'에서 '최대 3.0배' 범위 내로 억제합니다.
                float powerMultiplier = rodPower / fishStrength;
                powerMultiplier = Mathf.Clamp(powerMultiplier, 0.25f, 3.0f); 

                float finalStaminaDamage = fishingRod.EffectiveDamageRate * powerMultiplier;
                hookedFish.CurrentStamina -= finalStaminaDamage * Time.deltaTime;
                
                fishingRod.LineStress.Value -= fishingRod.stressDecreaseRate * Time.deltaTime;
            }
            else
            {
                fishingRod.LineStress.Value += fishingRod.stressIncreaseRate * Time.deltaTime;
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
                FailMiniGame("💥 물고기를 놓쳐 줄이 끊어졌습니다!");
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

            // 물고기 고유 agility(민첩)와 낚싯대 민첩 상쇄(EffectiveRodAgility)의 비율 계산
            float fishAgility = hookedFish.Data.agility > 0f ? hookedFish.Data.agility : 1f;
            float rodAgility = fishingRod.EffectiveRodAgility > 0f ? fishingRod.EffectiveRodAgility : 1f;

            // 물고기 민첩성 대비 장비 상쇄 비율 (예: 물고기 3 / 장비 6 = 0.5배 속도 상쇄)
            float speedMultiplier = fishAgility / rodAgility;
            
            // 안전 마진 한계치 클램핑 (0.2배 ~ 5.0배)
            speedMultiplier = Mathf.Clamp(speedMultiplier, 0.2f, 5.0f);

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

                    // 상쇄 배율이 곱해진 효율 시간(effectiveDuration)으로 이동 시간 계산
                    // 배율이 0.5배로 작아지면, 걸리는 시간은 2배로 늘어나 플레이어가 제압하기 용이해집니다.
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