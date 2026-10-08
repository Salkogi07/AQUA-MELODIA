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

        private float speedMultiplier = 1f;
        
        private const float BASE_FISHING_TIME = 40f;
        private float remainingFishingTime = 40f;
        private const float OVERTIME_STRESS_ACCELERATION = 0.08f;
        
        private float stressIncreaseMultiplier = 1f;
        private float stressDecreaseMultiplier = 1f;

        // 조준 허용 범위 반경 (UI 초록 범위 너비의 절반, 0~1 비율)
        private float sweetSpotHalfWidth;

        // 휠 입력이 누적되는 목표 위치 (PlayerReelRatio가 부드럽게 따라감)
        private float targetReelRatio;

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
            fishingRod.SetLineState(Fishing_Rod.FishingLineState.Taut);

            // 1. 게이지 및 타이머 초기화
            remainingFishingTime = BASE_FISHING_TIME;
            fishingRod.RemainingTime.Value = remainingFishingTime;
            fishingRod.EscapeTimerRatio.Value = 1f;

            fishingRod.PlayerReelRatio.Value = 0.5f;
            targetReelRatio = 0.5f;
            fishingRod.LineStress.Value = 0f;
            fishingRod.FishHpRatio.Value = 1f;
            fishingRod.IsMiniGameActive.Value = true;
            
            currentReelingAnimValue = 0f;
            reelingTimer = 0f;
            anim.SetFloat("reeling", 0f);

            // 2. 민첩 상쇄 계산
            float fishAgility = hookedFish.Data.agility > 0f ? hookedFish.Data.agility : 0f;
            float rodAgility = fishingRod.EffectiveRodAgility > 0f ? fishingRod.EffectiveRodAgility : 0f;
            float remainingAgility = Mathf.Max(0f, fishAgility - rodAgility);
            speedMultiplier = Mathf.Clamp(1f + (remainingAgility * 0.2f), 1.0f, 5.0f);

            // 조준 허용 범위 (낚싯대 sweetSpotBonus로 넓어지고, 물고기가 빠를수록 좁아짐)
            sweetSpotHalfWidth = fishingRod.EffectiveSweetSpotTolerance / speedMultiplier;
            fishingRod.SweetSpotSize.Value = sweetSpotHalfWidth * 2f;

            // 3. 탄성 vs 저항 상호작용 계산
            CalculateElasticityResistance();

            FishMovementRoutineAsync(cts.Token).Forget();
        }

        private void CalculateElasticityResistance()
        {
            float fishResist = hookedFish.Data.resistance;
            float rodElasticity = fishingRod.EffectiveRodElasticity;
            float diff = fishResist - rodElasticity;

            if (diff > 0f)
            {
                // [저항 > 탄성] 플레이어 조작 실패 시 스트레스 증가 속도 가속
                stressIncreaseMultiplier = 1f + (diff * 0.08f);
                stressDecreaseMultiplier = 1f;
            }
            else
            {
                // [탄성 >= 저항] 스트레스 증가 속도 완화(약간 감소) & 줄어드는 속도 증가
                float advantage = Mathf.Abs(diff);
                stressIncreaseMultiplier = Mathf.Max(0.5f, 1f - (advantage * 0.04f));
                stressDecreaseMultiplier = 1f + (advantage * 0.1f);
            }
        }

        public override void Update()
        {
            if (centerTransitionProgress < 1f)
            {
                centerTransitionProgress += Time.deltaTime / transitionDuration;
                if (centerTransitionProgress > 1f) centerTransitionProgress = 1f;
            }

            UpdateTimer();
            HandleInput();
            HandleRules();
            UpdateVisuals();
        }

        // 💡 [신규] 40초 타이머 및 시간 초과 페널티 처리
        private void UpdateTimer()
        {
            remainingFishingTime -= Time.deltaTime;
            fishingRod.RemainingTime.Value = remainingFishingTime;
            fishingRod.EscapeTimerRatio.Value = Mathf.Clamp01(remainingFishingTime / BASE_FISHING_TIME);

            // 40초 초과 시 스트레스 자연 증가 페널티 (시간이 지날수록 점점 더 빠르게 누적)
            if (remainingFishingTime < 0f)
            {
                float overtime = Mathf.Abs(remainingFishingTime);
                // 탄성/저항 계수 영향을 받지 않는 독립적인 자연 증가 페널티
                float overtimeNaturalStress = overtime * OVERTIME_STRESS_ACCELERATION * Time.deltaTime;
                fishingRod.LineStress.Value += overtimeNaturalStress;
            }
        }

        private void HandleInput()
        {
            float scrollDelta = Input.mouseScrollDelta.y;

            if (scrollDelta != 0)
            {
                // 초록 범위 가장자리가 바 끝에 닿을 때까지 이동 가능
                float minReel = Mathf.Min(sweetSpotHalfWidth, 0.5f);
                targetReelRatio = Mathf.Clamp(targetReelRatio + scrollDelta * fishingRod.wheelSensitivity, minReel, 1f - minReel);
                reelingTimer = reelingTimeout;
            }

            // 휠 틱 단위 점프 대신 목표 위치로 부드럽게 보간 (프레임레이트 독립)
            float smoothT = 1f - Mathf.Exp(-fishingRod.reelSmoothSpeed * Time.deltaTime);
            fishingRod.PlayerReelRatio.Value = Mathf.Lerp(fishingRod.PlayerReelRatio.Value, targetReelRatio, smoothT);

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

            float playerPos = fishingRod.PlayerReelRatio.Value;
            fishingRod.SweetSpotMin.Value = Mathf.Clamp01(playerPos - sweetSpotHalfWidth);
            fishingRod.SweetSpotMax.Value = Mathf.Clamp01(playerPos + sweetSpotHalfWidth);

            // 물고기 아이콘 중심이 초록 범위 안에 있으면 성공
            float difference = Mathf.Abs(playerPos - fishingRod.FishUiRatio.Value);
            bool isInsideSweetSpot = difference <= sweetSpotHalfWidth;

            if (isInsideSweetSpot)
            {
                // 체력 깎기
                float fishStrength = hookedFish.Data.strength > 0f ? hookedFish.Data.strength : 1f;
                float rodPower = fishingRod.EffectiveRodPower;
                
                float powerMultiplier = Mathf.Clamp(rodPower / fishStrength, 0.25f, 3.0f);
                float finalStaminaDamage = fishingRod.EffectiveDamageRate * powerMultiplier;
                hookedFish.CurrentStamina -= finalStaminaDamage * Time.deltaTime;
                
                // 💡 [탄성 효과 적용] 조준 성공 시 스트레스 감소 속도 증폭
                fishingRod.LineStress.Value -= (fishingRod.stressDecreaseRate * stressDecreaseMultiplier) * Time.deltaTime;
            }
            else
            {
                // 💡 [저항/탄성 효과 적용] 조준 실패 시 스트레스 증가 속도 보정
                float dynamicStressIncrease = (fishingRod.stressIncreaseRate * speedMultiplier) * stressIncreaseMultiplier;
                fishingRod.LineStress.Value += dynamicStressIncrease * Time.deltaTime;
            }

            fishingRod.LineStress.Value = Mathf.Clamp01(fishingRod.LineStress.Value);
            fishingRod.FishHpRatio.Value = hookedFish.CurrentStamina / hookedFish.Data.maxStamina;

            // 물고기 기력 소진 -> 발악 상태 전환
            if (hookedFish.CurrentStamina <= 0)
            {
                cts?.Cancel();
                stateMachine.ChangeState(fishingRod.FinalStruggleState);
                return;
            }

            // 낚싯줄 터짐 실패 (시간 초과 또는 잦은 조준 실패로 100% 도달)
            if (fishingRod.LineStress.Value >= 1f)
            {
                FailMiniGame("💥 물고기가 거칠게 저항하여 낚싯줄이 압력을 견디지 못하고 끊어졌습니다!");
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