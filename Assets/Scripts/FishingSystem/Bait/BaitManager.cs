using UnityEngine;

namespace FishingSystem.Bait
{
    public class BaitManager : MonoBehaviour
    {
        public static BaitManager Instance { get; private set; }

        [Header("실시간 장착 미끼 슬롯")]
        [Tooltip("현재 장착하고 있는 미끼 데이터를 인스펙터 창에서 실시간 확인/변경합니다.")]
        [SerializeField] private BaitDataSO equippedBait;

        // 현재 장착 중인 미끼 데이터를 반환하는 읽기 전용 프로퍼티
        public BaitDataSO CurrentBait => equippedBait;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 미끼를 장착합니다 (BaitDataSO에 null 전달 시 해제)
        /// </summary>
        public void EquipBait(BaitDataSO bait)
        {
            equippedBait = bait;
            Debug.Log($"🪱 [미끼 변경] 현재 장착된 미끼: {(bait != null ? bait.baitName : "없음")}");
        }
    }
}