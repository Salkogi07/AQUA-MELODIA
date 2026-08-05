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

        public void ClearList()
        {
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public void AddFishSlot(FishData fish)
        {
            GameObject slotObj = Instantiate(fishSlotPrefab, listContainer);
            if (slotObj.TryGetComponent<FishSlotUI>(out var slotUI))
            {
                slotUI.SetupSlot(fish);
            }
            else
            {
                Debug.LogWarning($"⚠️ 생성된 슬롯 프리팹({slotObj.name})에 'FishSlotUI' 스크립트가 없습니다.");
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