using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fishing_Rod;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class RetrievingState : FishingState
    {
        public RetrievingState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            fishingRod.SetLineState(FishingLineState.Taut);
            fishingRod.BobberRb.bodyType = RigidbodyType2D.Dynamic;
            Debug.Log("<color=#99E6FF>🎣 릴을 감아 찌를 회수합니다...</color>");
        }

        public override void Update()
        {
            Vector3 targetPos = fishingRod.GetTargetStartPosition();
            float distance = Vector3.Distance(fishingRod.bobber.position, targetPos);

            if (distance < 0.6f)
            {
                Debug.Log("<color=white>📥 찌 회수 완료.</color>");
                stateMachine.ChangeState(fishingRod.ReadyState);
            }
            else
            {
                Vector2 pullDirection = (targetPos - fishingRod.bobber.position).normalized;
                fishingRod.BobberRb.linearVelocity = pullDirection * fishingRod.reelInSpeed;
            }
        }
    }
}