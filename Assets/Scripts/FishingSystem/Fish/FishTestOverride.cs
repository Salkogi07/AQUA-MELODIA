using System.Collections.Generic;
using UnityEngine;

namespace FishingSystem.Fish
{
    /// <summary>
    /// 테스트 씬 전용: 씬에 있으면 낚시터 확률 추첨 대신 지정한 물고기 중에서만 입질이 옵니다.
    /// </summary>
    public class FishTestOverride : MonoBehaviour
    {
        public static FishTestOverride Instance { get; private set; }

        [Tooltip("테스트할 물고기 목록 (여러 개면 무작위로 하나 선택, 비어 있으면 원래 낚시터 추첨 사용)")]
        public List<FishDataSO> testFishList = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public FishDataSO PickFish()
        {
            var candidates = testFishList.FindAll(f => f != null);
            if (candidates.Count == 0) return null;

            FishDataSO picked = candidates[Random.Range(0, candidates.Count)];
            Debug.Log($"<color=lime>🧪 [테스트 물고기 지정]</color> {picked.fishName}");
            return picked;
        }
    }
}
