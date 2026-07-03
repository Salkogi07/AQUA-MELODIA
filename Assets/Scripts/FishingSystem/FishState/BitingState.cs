using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fishing_Rod;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class BitingState : FishingState
    {
        private CancellationTokenSource cts;
        private bool clicked = false;

        public BitingState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            clicked = false;
            cts = new CancellationTokenSource();
            
            fishingRod.SetLineState(FishingLineState.Taut);
            fishingRod.ApplyBitePhysics();
            
            Debug.Log($"<color=yellow>⏰ 찌가 들어갔습니다! {fishingRod.inputTimeLimit}초 안에 챔질하세요!!</color>");
            WaitForReactionAsync(cts.Token).Forget();
        }

        public override void Update()
        {
            if (Input.GetMouseButtonDown(0) && !clicked)
            {
                clicked = true;
                cts?.Cancel(); // 타이머 취소

                Debug.Log($"<color=#00FF00>⚔️ [챔질 성공] {fishingRod.CurrentHookedFish.Data.fishName}가 걸렸습니다! 미니게임 시작!</color>");

                // 회수(Retrieving)가 아닌 미니게임(MiniGame) 상태로 전환!
                stateMachine.ChangeState(fishingRod.MiniGameState);
            }
        }

        private async UniTaskVoid WaitForReactionAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(fishingRod.inputTimeLimit), cancellationToken: token);
                
                // 시간 초과 (실패)
                Debug.Log($"<color=#AAAAAA>💨 [놓침] 물고기가 도망쳤습니다.</color>");
                fishingRod.CurrentHookedFish = null;
                fishingRod.StopBobberMovement(); 
                fishingRod.SetLineState(FishingLineState.Slack);
                
                // 다시 입질 대기 상태로 복귀
                stateMachine.ChangeState(fishingRod.SettledState);
            }
            catch (System.OperationCanceledException) 
            { 
                // 클릭해서 성공한 경우 이곳으로 옴
            }
        }

        public override void Exit()
        {
            base.Exit();
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}