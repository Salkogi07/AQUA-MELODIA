using UnityEngine;
using UnityEngine.UI;
using FishingSystem.Fish;

namespace FishingSystem.UI
{
    public class FishChestUIView : MonoBehaviour
    {
        [Header("목록 표시용 프리팹 템플릿")]
        [SerializeField] private GameObject fishSlotPrefab;
        [SerializeField] private Transform listContainer;

        [Header("용량 표시")]
        [SerializeField] private Text capacityText;

        [Header("연결할 툴팁 UI")]
        [SerializeField] private FishTooltipUI tooltipUI;

        public void ClearList()
        {
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public void AddFishSlot(FishData fish)
        {
            if (fish == null) return;

            GameObject slotObj = Instantiate(fishSlotPrefab, listContainer);
            if (slotObj.TryGetComponent<FishSlotUI>(out var slotUI))
            {
                slotUI.SetupSlot(fish, tooltipUI);
            }
            else
            {
                Destroy(slotObj);
                Debug.LogWarning($"⚠️ 생성된 슬롯 프리팹({slotObj.name})에 'FishSlotUI' 스크립트가 없습니다.");
            }
        }

        // 개별 인벤토리 슬롯 삭제 기능 구현
        public void RemoveFishSlot(FishData fish)
        {
            if (fish == null) return;

            foreach (Transform child in listContainer)
            {
                if (child.TryGetComponent<FishSlotUI>(out var slotUI))
                {
                    // 메모리 주소가 일치하는 고유 인스턴스인지 명시적 참조 비교 수행
                    if (object.ReferenceEquals(slotUI.CurrentFishData, fish))
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }

        public void UpdateCapacityText(int currentCount, int maxCapacity)
        {
            if (capacityText != null)
            {
                capacityText.text = $"{currentCount} / {maxCapacity}";
            }
        }
    }
}