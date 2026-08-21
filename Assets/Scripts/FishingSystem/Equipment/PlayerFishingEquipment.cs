using UnityEngine;

namespace FishingSystem.Equipment
{
    public class PlayerFishingEquipment : MonoBehaviour
    {
        public static PlayerFishingEquipment Instance { get; private set; }

        [Header("장착 슬롯")]
        [SerializeField] private FishingRodDataSO equippedRod;
        
        public FishingRodDataSO EquippedRod => equippedRod;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        /// <summary>
        /// 함수를 실행하여 새로운 낚싯대로 교체 장착합니다.
        /// </summary>
        public void EquipRod(FishingRodDataSO newRod)
        {
            if (newRod == null) return;
            equippedRod = newRod;
            Debug.Log($"🎣 [장비 교체] {equippedRod.rodName} 로드를 장착했습니다!");
            
            // 필요 시 장착 변화에 따른 즉각적인 물리값 재계산 등을 이곳에 바로 작성할 수 있습니다.
        }
    }
}