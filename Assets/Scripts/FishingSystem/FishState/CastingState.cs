using UnityEngine;

namespace FishingSystem.FishState
{
    public class CastingState : FishingState
    {
        private bool hasThrown = false; // 찌가 날아갔는지 확인하는 변수

        public CastingState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter(); 
            hasThrown = false; 
            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Slack);
        }

        public void ExecuteCast()
        {
            hasThrown = true;
            fishingRod.ApplyCastPhysics(); 
            Debug.Log($"<color=lime>🚀 캐스팅 발사! (애니메이션 싱크 완료)</color>");
        }

        public override void Update()
        {
            // 💡 아직 애니메이션에서 던지는 타이밍이 안 왔다면(던지기 전)
            if (!hasThrown) 
            {
                // 낚싯대를 휘두르는 동안 찌가 낚싯대 끝(또는 손)을 계속 따라가도록 고정시킵니다.
                fishingRod.bobber.position = fishingRod.GetTargetStartPosition();
                return; // 아래 로직(날아가는 로직)은 실행하지 않고 대기
            }

            // 날아가는 도중 좌클릭 시 즉시 회수
            if (Input.GetMouseButtonDown(0))
            {
                stateMachine.ChangeState(fishingRod.RetrievingState);
                return;
            }

            // 속도가 줄어들면 물에 안착한 것으로 판단
            if (fishingRod.BobberRb.linearVelocity.magnitude < 0.2f)
            {
                stateMachine.ChangeState(fishingRod.SettledState);
            }
        }
    }
}