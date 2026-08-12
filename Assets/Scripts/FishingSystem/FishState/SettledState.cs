using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fish;
using FishingSystem.Input_Helper;

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
            
            fishingRod.StopBobberMovement(); 

            // 착수한 위치에서 실제 수중 레이어 구역(Collider)을 다시 획득하여 할당합니다.
            fishingRod.ActiveFishingZone = fishingRod.SearchFishingZone();
            
            if (fishingRod.ActiveFishingZone != null)
            {
                // 착수 구역 기준으로 가로 좌표계 실시간 매핑 동기화
                fishingRod.UpdateZoneMapping(
                    -fishingRod.ActiveFishingZone.maxMoveRange,
                    fishingRod.ActiveFishingZone.maxMoveRange,
                    fishingRod.ActiveFishingZone.transform.position.x
                );
            }
            
            WaitAndBiteAsync(cts.Token).Forget();
        }

        public override void Update()
        {
            if (FishingInput.GetLeftClickDown())
            {
                stateMachine.ChangeState(fishingRod.RetrievingState);
            }
        }

        private async UniTaskVoid WaitAndBiteAsync(CancellationToken token)
        {
            FishingZone foundZone = fishingRod.ActiveFishingZone;
            if (foundZone == null)
            {
                Debug.LogWarning("⚠️ 찌가 낚시 구역(FishingZone Collider) 바깥에 착수하여 입질이 오지 않습니다.");
                return; 
            }

            FishDataSO foundFish = foundZone.GetRandomFish();
            if (foundFish == null) return;

            try
            {
                float waitTime = Random.Range(fishingRod.biteDelayRange.x, fishingRod.biteDelayRange.y);
                await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                fishingRod.CurrentHookedFish = new FishData(foundFish);
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