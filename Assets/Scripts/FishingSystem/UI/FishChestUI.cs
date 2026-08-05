using UnityEngine;
using UnityEngine.UI;
using FishingSystem.Data;

namespace FishingSystem.UI
{
    public class FishChestUI : MonoBehaviour
    {
        [Header("목록 표시용 프리팹 템플릿")]
        [Tooltip("FishSlotUI 스크립트가 최상단 컴포넌트로 조립된 슬롯 프리팹이어야 합니다.")]
        [SerializeField] private GameObject fishSlotPrefab;
        [SerializeField] private Transform listContainer;

        [Header("용량 표시")]
        [SerializeField] private Text capacityText;

        private void OnEnable()
        {
            RefreshStoredFishList();
        }

        public void RefreshStoredFishList()
        {
            // 기존 렌더링되어 있던 자식 스크롤 카드들 초기 청소
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }

            var manager = FishingDataManager.Instance;
            if (manager == null) return;

            // 보관된 물고기 개수만큼 슬롯 아이템 생성 및 주입
            foreach (var fish in manager.StoredFish)
            {
                GameObject slotObj = Instantiate(fishSlotPrefab, listContainer);
                
                // 신규 전용 컴포넌트(FishSlotUI)를 호출하여 텍스트 및 스프라이트 처리 위임
                if (slotObj.TryGetComponent<FishSlotUI>(out var slotUI))
                {
                    slotUI.SetupSlot(fish);
                }
                else
                {
                    Debug.LogWarning($"⚠️ 생성된 슬롯 프리팹({slotObj.name})에 'FishSlotUI' 스크립트 조립이 누락되었습니다.");
                }
            }

            // 용량 텍스트 출력
            if (capacityText != null)
            {
                capacityText.text = $"{manager.StoredFish.Count} / {manager.MaxCapacity}";
            }
        }
    }
}