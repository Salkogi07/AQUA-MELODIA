using UnityEngine;

namespace FishingSystem.Equipment
{
    public class PlayerFishingEquipment : MonoBehaviour
    {
        [Header("장착 슬롯")]
        [SerializeField] private FishingRodDataSO equippedRod;
        
        public FishingRodDataSO EquippedRod => equippedRod;

        /// <summary>
        /// 새로운 낚싯대로 교체 장착합니다.
        /// </summary>
        public void EquipRod(FishingRodDataSO newRod)
        {
            if (newRod == null) return;
            equippedRod = newRod;
            Debug.Log($"🎣 [장비 교체] {equippedRod.rodName} 로드를 장착했습니다!");
        }
    }
}