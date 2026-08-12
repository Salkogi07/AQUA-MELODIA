using UnityEngine;
using FishingSystem.Input_Helper;

namespace FishingSystem.FishState
{
    public class CastingState : FishingState
    {
        private bool hasThrown = false; 
        private float savedPower = 0f;

        public CastingState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter(); 
            hasThrown = false; 
            savedPower = fishingRod.CastPower.Value;
            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Slack);
        }

        public void ExecuteCast()
        {
            hasThrown = true;
            fishingRod.ApplyCastPhysics(savedPower); 
            Debug.Log($"<color=lime>🚀 물리 탄도 캐스팅 발사! 세기: {savedPower * 100f:F1}%</color>");
        }

        public override void Update()
        {
            if (!hasThrown) 
            {
                fishingRod.bobber.position = fishingRod.GetTargetStartPosition();
                return; 
            }

            if (FishingInput.GetLeftClickDown())
            {
                stateMachine.ChangeState(fishingRod.RetrievingState);
                return;
            }

            // 비행 범위 자동 억제 (텔레포트 및 오버슈트 방지)
            if (fishingRod.ActiveFishingZone != null)
            {
                float minX = fishingRod.ActiveFishingZone.transform.position.x - fishingRod.ActiveFishingZone.maxMoveRange;
                float maxX = fishingRod.ActiveFishingZone.transform.position.x + fishingRod.ActiveFishingZone.maxMoveRange;
                
                Vector3 pos = fishingRod.bobber.position;
                float xVelocity = fishingRod.BobberRb.linearVelocity.x;

                if (xVelocity > 0.01f)
                {
                    pos.x = Mathf.Min(pos.x, maxX);
                }
                else if (xVelocity < -0.01f)
                {
                    pos.x = Mathf.Max(pos.x, minX);
                }
                
                fishingRod.bobber.position = pos;
            }

            // 찌가 수면 마찰(Damping)에 의해 정상적으로 멈췄을 때 안착 판정을 수행하여 오작동 방지
            if (fishingRod.BobberRb.linearVelocity.magnitude < 0.2f)
            {
                stateMachine.ChangeState(fishingRod.SettledState);
            }
        }
    }
}