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

            // 착수한 위치에서 실제 수중 레이어 구역(Collider)을 획득하여 할당합니다.
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

            try
            {
                // 1. 물고기가 찌 주변으로 다가오는 대기 시간을 먼저 보냅니다.
                float waitTime = Random.Range(fishingRod.biteDelayRange.x, fishingRod.biteDelayRange.y);
                await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                // 2. 대기가 끝나고 찌를 물기 직전(BitingState 진입 직전)에 실제로 물고기를 추첨합니다.
                FishDataSO foundFish = foundZone.GetRandomFish();
                if (foundFish == null)
                {
                    Debug.LogWarning("⚠️ 입질 타이밍에 물고기를 추첨하는 데 실패했습니다. 조건에 맞는 물고기 데이터가 없거나 확률 설정 오류일 수 있습니다.");
                    return;
                }

                // 3. 당첨된 물고기 데이터를 생성하고 입질 상태로 전환합니다.
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