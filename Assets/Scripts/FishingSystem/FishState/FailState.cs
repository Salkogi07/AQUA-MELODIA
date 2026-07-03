using UnityEngine;
using FishingSystem.Fishing_Rod;

namespace FishingSystem.FishState
{
    public class FailedState : FishingState
    {
        public FailedState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter(); // "IsFailed" 애니메이션 파라미터가 True가 됨
            
            // 실패 시 즉시 낚싯줄을 느슨하게 하고 찌 물리 초기화
            fishingRod.SetLineState(FishingLineState.Slack);
            fishingRod.ResetBobberPhysics();
            
            Debug.Log("<color=gray>💦 낚시 실패... 실패 애니메이션을 재생합니다.</color>");
            
            // 애니메이션 종료 후 ReadyState로 돌아가는 것은
            // Animation Event가 OnAnimationEvent_FailFinished()를 호출하여 처리됩니다.
        }

        public override void Exit()
        {
            base.Exit(); // "IsFailed" 파라미터를 False로 변경
        }
    }
}