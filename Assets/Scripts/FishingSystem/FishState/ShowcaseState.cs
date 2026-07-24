using UnityEngine;

namespace FishingSystem.FishState
{
    public class ShowcaseState : FishingState
    {
        private GameObject fishObject;

        public ShowcaseState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public void SetFishObject(GameObject obj) => fishObject = obj;

        public override void Enter()
        {
            base.Enter();
            
            // 찌는 숨기고 물고기만 플레이어 손 위치로 이동
            fishingRod.bobber.gameObject.SetActive(false);

            if (fishObject != null)
            {
                fishObject.transform.SetParent(fishingRod.showcaseMountPoint);
                fishObject.transform.localPosition = Vector3.zero;
                fishObject.transform.localRotation = Quaternion.identity;
                fishObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }

            Debug.Log("<color=orange>✨ [자랑하기] 클릭하면 다시 준비 상태가 됩니다.</color>");
        }

        public override void Update()
        {
            // 플레이어가 클릭하면 종료
            if (Input.GetMouseButtonDown(0))
            {
                stateMachine.ChangeState(fishingRod.ReadyState);
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (fishObject != null) Object.Destroy(fishObject);
            
            fishingRod.bobber.gameObject.SetActive(true);
            fishingRod.CurrentHookedFish = null;
        }
    }
}