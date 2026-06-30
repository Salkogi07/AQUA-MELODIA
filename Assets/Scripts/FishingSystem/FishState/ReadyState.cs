using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fishing_Rod;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class ReadyState : FishingState
    {
        public ReadyState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            fishingRod.SetLineState(FishingLineState.Slack);
            fishingRod.CurrentHookedFish = null;
            fishingRod.ResetBobberPhysics();
            fishingRod.bobber.position = fishingRod.GetTargetStartPosition();
        }

        public override void Update()
        {
            // 플레이어 움직임에 따라 찌 위치 계속 고정
            fishingRod.bobber.position = fishingRod.GetTargetStartPosition();

            if (Input.GetMouseButtonDown(0))
            {
                stateMachine.ChangeState(fishingRod.CastingState);
            }
        }
    }
}