using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace FishingSystem.FishState
{
    public class CaughtState : FishingState
    {
        private CancellationTokenSource cts;
        private bool isPulled = false;
        private GameObject spawnedFish;

        public CaughtState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            isPulled = false;
            cts = new CancellationTokenSource();

            // 1. 물고기 비주얼 생성 및 바늘(찌)에 부착
            if (fishingRod.CurrentHookedFish != null)
            {
                spawnedFish = Object.Instantiate(fishingRod.fishVisualPrefab, fishingRod.fishHookPoint);
                spawnedFish.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                
                var sr = spawnedFish.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sprite = fishingRod.CurrentHookedFish.Data.fishSprite;
            }

            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Taut);
            Debug.Log("<color=lime>🎣 [성공] 물고기를 낚아 올립니다!</color>");
        }

        public override void Update()
        {
            if (!isPulled)
            {
                // 당기기 전까지 찌가 살짝 끌려오는 연출
                Vector2 dir = (fishingRod.rodTip.position - fishingRod.bobber.position).normalized;
                fishingRod.BobberRb.linearVelocity = Vector2.Lerp(fishingRod.BobberRb.linearVelocity, dir * 5f, Time.deltaTime * 5f);
            }
        }

        // 애니메이션 이벤트(OnAnimationEvent_PullBobber)에서 호출됨
        public void ExecutePull()
        {
            if (isPulled) return;
            isPulled = true;
            fishingRod.ResetBobberPhysics();
            FlyToPlayerRoutine(cts.Token).Forget();
        }

        private async UniTaskVoid FlyToPlayerRoutine(CancellationToken token)
        {
            Transform bobber = fishingRod.bobber;
            Transform targetHand = fishingRod.catchHandPosition;
            Vector3 startPos = bobber.position;
            float elapsed = 0f;

            try
            {
                while (elapsed < fishingRod.retrieveDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fishingRod.retrieveDuration;

                    // 포물선 이동
                    Vector3 p1 = (startPos + targetHand.position) / 2f + Vector3.up * fishingRod.retrieveArcHeight;
                    float u = 1 - t;
                    bobber.position = (u * u * startPos) + (2 * u * t * p1) + (t * t * targetHand.position);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                // 도착 시 자랑하기 상태로 전이
                fishingRod.ShowcaseState.SetFishObject(spawnedFish);
                stateMachine.ChangeState(fishingRod.ShowcaseState);
            }
            catch (System.OperationCanceledException) { }
        }

        public override void Exit()
        {
            base.Exit();
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}