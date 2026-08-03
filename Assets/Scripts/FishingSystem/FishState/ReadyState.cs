using UnityEngine;
using FishingSystem.Fishing_Rod;

namespace FishingSystem.FishState
{
    public class ReadyState : FishingState
    {
        private float chargeDirection = 1f;

        public ReadyState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            fishingRod.SetLineState(FishingLineState.Slack);
            fishingRod.CurrentHookedFish = null;
            fishingRod.ResetBobberPhysics();
            fishingRod.bobber.position = fishingRod.GetTargetStartPosition();

            fishingRod.CastPower.Value = 0f;
            fishingRod.IsCharging.Value = false;
            chargeDirection = 1f;
            fishingRod.ActiveFishingZone = null;
        }

        public override void Update()
        {
            fishingRod.bobber.position = fishingRod.GetTargetStartPosition();

            if (Input.GetMouseButtonDown(0))
            {
                fishingRod.ActiveFishingZone = fishingRod.FindNearestFishingZone();
                if (fishingRod.ActiveFishingZone != null)
                {
                    fishingRod.UpdateZoneMapping(
                        -fishingRod.ActiveFishingZone.maxMoveRange,
                        fishingRod.ActiveFishingZone.maxMoveRange,
                        fishingRod.ActiveFishingZone.transform.position.x
                    );
                }
            }

            if (Input.GetMouseButton(0))
            {
                if (!fishingRod.IsCharging.Value)
                {
                    fishingRod.IsCharging.Value = true;
                }

                fishingRod.CastPower.Value += Time.deltaTime * fishingRod.chargeSpeed * chargeDirection;

                if (fishingRod.CastPower.Value >= 1f)
                {
                    fishingRod.CastPower.Value = 1f;
                    chargeDirection = -1f;
                }
                else if (fishingRod.CastPower.Value <= 0f)
                {
                    fishingRod.CastPower.Value = 0f;
                    chargeDirection = 1f;
                }
            }

            if (Input.GetMouseButtonUp(0) && fishingRod.IsCharging.Value)
            {
                fishingRod.IsCharging.Value = false;
                stateMachine.ChangeState(fishingRod.CastingState);
            }
        }
    }
}