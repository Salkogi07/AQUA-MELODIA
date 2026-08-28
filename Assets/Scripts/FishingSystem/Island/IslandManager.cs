using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishingSystem.Data;
using FishingSystem.Fish;

namespace FishingSystem.Island
{
    public class IslandManager : MonoBehaviour
    {
        public static IslandManager Instance { get; private set; }

        // 게임 실행 중 메모리에만 유지되는 해금된 섬 ID 목록
        private readonly HashSet<string> _unlockedIslandIds = new();

        // 테스트용 현재 플레이어 소지 골드 (기존 재화 시스템이 있다면 연동)
        public int CurrentPlayerGold { get; set; } = 5000; 

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
            }
        }

        public bool IsUnlocked(IslandDataSO island)
        {
            if (island == null) return false;
            return _unlockedIslandIds.Contains(island.islandId);
        }

        public bool CanUnlock(IslandDataSO island)
        {
            if (island == null) return false;
            if (IsUnlocked(island)) return false;

            // 골드 체크
            if (CurrentPlayerGold < island.requiredGold) return false;

            // 필수 물고기 도감 등록 체크
            if (FishingDataManager.Instance == null) return false;
            foreach (var reqFish in island.requiredFishList)
            {
                if (reqFish == null) continue;
                if (!IsFishInEncyclopedia(reqFish)) return false;
            }

            return true;
        }

        public bool TryUnlockIsland(IslandDataSO island)
        {
            if (!CanUnlock(island))
            {
                Debug.LogWarning($"⚠️ {island.islandName} 해금 조건 미충족");
                return false;
            }

            CurrentPlayerGold -= island.requiredGold;
            _unlockedIslandIds.Add(island.islandId);
            Debug.Log($"🔓 {island.islandName} 해금 완료");
            return true;
        }

        private bool IsFishInEncyclopedia(FishDataSO targetFish)
        {
            if (FishingDataManager.Instance == null) return false;
            foreach (var kvp in FishingDataManager.Instance.Encyclopedia)
            {
                if (kvp.Key != null && kvp.Key.Data == targetFish) return true;
            }
            return false;
        }
    }
}