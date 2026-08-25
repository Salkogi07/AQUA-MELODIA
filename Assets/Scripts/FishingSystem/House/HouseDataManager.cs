using System.Collections.Generic;
using UnityEngine;
using FishingSystem.Fish;

namespace FishingSystem.House
{
    public class HouseDataManager : MonoBehaviour
    {
        public static HouseDataManager Instance { get; private set; }

        [Header("하우스 어항 보관 사양")]
        [SerializeField] private int _maxCapacity = 15;
        public int MaxCapacity => _maxCapacity;

        private readonly List<FishData> _housedFish = new();
        public IReadOnlyList<FishData> HousedFish => _housedFish;

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

        /// <summary>
        /// 어항에 물고기를 추가합니다.
        /// </summary>
        public bool TryAddFishToHouse(FishData fish)
        {
            if (_housedFish.Count >= _maxCapacity)
            {
                Debug.LogWarning("🏠 어항 수용 공간이 부족하여 물고기를 추가하지 못했습니다.");
                return false;
            }

            _housedFish.Add(fish);
            Debug.Log($"🏠 [어항 보관] 새로운 물고기 수납 완료: {fish.Data.fishName}");
            return true;
        }

        /// <summary>
        /// 어항에서 물고기를 제거(회수)합니다.
        /// </summary>
        public bool TryRemoveFishFromHouse(FishData fish)
        {
            if (_housedFish.Remove(fish))
            {
                Debug.Log($"🏠 [어항 회수] 어항에서 물고기 회수 완료: {fish.Data.fishName}");
                return true;
            }
            return false;
        }
    }
}