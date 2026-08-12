using UnityEngine;
using UnityEngine.EventSystems;
using FishingSystem.Fishing_Rod;
using FishingSystem.Input_Helper;

namespace FishingSystem.FishState
{
    public class ReadyState : FishingState
    {
        private float chargeDirection = 1f;
        
        // [추가] 이전 상태(자랑하기 등)의 닫기 클릭이 캐스팅으로 이어지는 번짐 현상을 방지하는 쿨다운
        private float inputCooldownTimer = 0f;
        private const float COOLDOWN_DURATION = 0.2f; 

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

            // 진입 시점에 즉시 쿨다운 적용
            inputCooldownTimer = COOLDOWN_DURATION;
        }

        public override void Update()
        {
            fishingRod.bobber.position = fishingRod.GetTargetStartPosition();

            // 쿨다운 실행 중에는 인게임 클릭 일체 차단
            if (inputCooldownTimer > 0f)
            {
                inputCooldownTimer -= Time.deltaTime;
                return;
            }

            // 1. 최초 클릭 다운 시점 필터링
            if (FishingInput.GetLeftClickDown())
            {
                // UI를 터치하거나 클릭하고 있는 경우 캐스팅 로직 진입을 거부합니다.
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                fishingRod.ActiveFishingZone = fishingRod.FindNearestFishingZone();
                if (fishingRod.ActiveFishingZone != null)
                {
                    fishingRod.UpdateZoneMapping(
                        -fishingRod.ActiveFishingZone.maxMoveRange,
                        fishingRod.ActiveFishingZone.maxMoveRange,
                        fishingRod.ActiveFishingZone.transform.position.x
                    );

                    // UI 클릭이 아니며, 유효한 낚시터 영역이 있을 때만 충전 개시 허가
                    fishingRod.IsCharging.Value = true; 
                }
            }

            // 2. 충전이 허가된 상태에서만 마우스 홀드 연산 작동
            if (Input.GetMouseButton(0) && fishingRod.IsCharging.Value)
            {
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

            // 3. 충전 중인 상태에서 마우스를 뗄 때에만 캐스팅 전개
            if (FishingInput.GetLeftClickUp() && fishingRod.IsCharging.Value)
            {
                fishingRod.IsCharging.Value = false;
                stateMachine.ChangeState(fishingRod.CastingState);
            }
        }
    }
}