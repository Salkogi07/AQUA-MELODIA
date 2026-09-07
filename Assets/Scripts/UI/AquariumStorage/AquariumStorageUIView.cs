using UnityEngine;
using UnityEngine.UI;
using FishingSystem.Fish;
using FishingSystem.House;

namespace UI.AquariumStorage
{
    public class AquariumStorageUIView : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private GameObject dragSlotPrefab;
        [SerializeField] private Transform listContainer;
        [SerializeField] private Text capacityText;
        [SerializeField] private Canvas rootCanvas; // 드래그 시 부모가 될 최상위 캔버스

        public void ClearList()
        {
            foreach (Transform child in listContainer)
                Destroy(child.gameObject);
        }

        public void AddFishSlot(FishData fish, Aquarium aquarium)
        {
            if (fish == null) return;

            GameObject slotObj = Instantiate(dragSlotPrefab, listContainer);
            if (slotObj.TryGetComponent<AquariumStorageSlotUI>(out var slotUI))
            {
                slotUI.Setup(fish, rootCanvas, aquarium);
            }
        }

        public void UpdateCapacityText(int current, int max)
        {
            if (capacityText != null)
                capacityText.text = $"{current} / {max}";
        }
    }
}