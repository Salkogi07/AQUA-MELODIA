using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class SettledState : FishingState
    {
        private CancellationTokenSource cts;

        public SettledState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            cts = new CancellationTokenSource();
            WaitAndBiteAsync(cts.Token).Forget();
        }

        public override void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                stateMachine.ChangeState(fishingRod.RetrievingState);
            }
        }

        private async UniTaskVoid WaitAndBiteAsync(CancellationToken token)
        {
            // 1. 낚시 구역 자체를 가져옵니다.
            FishingZone foundZone = fishingRod.SearchFishingZone();
            if (foundZone == null) return; // 낚시존이 없으면 입질 안 옴

            // 2. 구역에서 물고기 데이터를 뽑습니다.
            FishDataSO foundFish = foundZone.GetRandomFish();
            if (foundFish == null) return;

            try
            {
                float waitTime = Random.Range(fishingRod.biteDelayRange.x, fishingRod.biteDelayRange.y);
                await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                fishingRod.CurrentHookedFish = new FishData(foundFish);

                // 💡 3. 미니게임에 낚시터의 크기를 반영 (덮어쓰기)
                // maxMoveRange가 5f라면 minX는 -5f, maxX는 5f가 됩니다.
                fishingRod.patternMinX = -foundZone.maxMoveRange;
                fishingRod.patternMaxX = foundZone.maxMoveRange;
                fishingRod.currentZoneCenterX = foundZone.transform.position.x;

                // 물고기가 물었으므로 상태 전환
                stateMachine.ChangeState(fishingRod.BitingState); 
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