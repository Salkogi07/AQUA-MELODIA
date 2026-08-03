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
        
        // 정렬 보간용 변수
        private Vector3 landedBobberPosition;    
        private float centerTransitionProgress = 0f; 
        private const float transitionDuration = 1.0f; // 정렬 시간 (1초)

        // 릴링 애니메이션 제어용 변수
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
            bool isInsideSweetSpot = difference <= fishingRod.sweetSpotTolerance;

            if (isInsideSweetSpot)
            {
                hookedFish.CurrentStamina -= fishingRod.fishDamageRate * Time.deltaTime;
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

                    if (duration > 0f)
                    {
                        while (timeElapsed < duration)
                        {
                            timeElapsed += Time.deltaTime;
                            float t = Mathf.Clamp01(timeElapsed / duration);
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