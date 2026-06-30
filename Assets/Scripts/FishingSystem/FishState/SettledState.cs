using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fishing_Rod;
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
            // 대기 중에 좌클릭하면 그냥 찌를 회수함
            if (Input.GetMouseButtonDown(0))
            {
                stateMachine.ChangeState(fishingRod.RetrievingState);
            }
        }

        private async UniTaskVoid WaitAndBiteAsync(CancellationToken token)
        {
            FishDataSO foundFish = fishingRod.SearchFishingZone();
            if (foundFish == null) return; // 낚시존이 없으면 입질 안 옴

            try
            {
                float waitTime = Random.Range(fishingRod.biteDelayRange.x, fishingRod.biteDelayRange.y);
                await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                fishingRod.CurrentHookedFish = new FishData(foundFish);
                stateMachine.ChangeState(fishingRod.BitingState); // 물고기가 물면 Biting 상태로 전환
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