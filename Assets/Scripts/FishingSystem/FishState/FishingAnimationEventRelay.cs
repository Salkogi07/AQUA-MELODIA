using UnityEngine;

namespace FishingSystem.FishState
{
    // 이 스크립트를 Animator가 붙어있는 게임 오브젝트에 드래그해서 넣으세요!
    public class FishingAnimationEventRelay : MonoBehaviour
    {
        private FishingRod fishingRod;

        private void Awake()
        {
            // 자신을 포함해 부모 오브젝트들 중에서 FishingRod 스크립트를 찾아 연결합니다.
            fishingRod = GetComponentInParent<FishingRod>();
            
            if (fishingRod == null)
            {
                Debug.LogError("부모 오브젝트에서 FishingRod 스크립트를 찾을 수 없습니다!");
            }
        }

        // 애니메이션 이벤트에서 이 함수를 실행하게 됩니다.
        public void OnAnimationEvent_ThrowBobber()
        {
            if (fishingRod != null)
            {
                // 부모에 있는 진짜 함수를 대신 호출(토스)해줍니다.
                fishingRod.OnAnimationEvent_ThrowBobber();
            }
        }
        
        // 낚시 실패 애니메이션 끝부분 이벤트
        public void OnAnimationEvent_FailFinished()
        {
            if (fishingRod != null)
            {
                fishingRod.OnAnimationEvent_FailFinished();
            }
        }
    }
}