using System.Collections.Generic;
using UnityEngine;
using R3; // [추가] R3 기능 탑재
using FishingSystem.Fish;

namespace FishingSystem.Data
{
    public class FishingDataManager : MonoBehaviour
    {
        public static FishingDataManager Instance { get; private set; }

        [Header("인벤토리 사양")]
        [SerializeField] private int _maxCapacity = 10;
        public int MaxCapacity => _maxCapacity;

        private readonly List<FishData> _storedFish = new();
        public IReadOnlyList<FishData> StoredFish => _storedFish;

        private readonly Dictionary<FishData, int> _encyclopedia = new();
        public IReadOnlyDictionary<FishData, int> Encyclopedia => _encyclopedia;

        private readonly Dictionary<FishData, float> _maxRecords = new();
        
        private readonly Subject<FishData> _onFishAdded = new();
        public Observable<FishData> OnFishAdded => _onFishAdded;
        private readonly Subject<FishData> _onFishRemoved = new();
        public Observable<FishData> OnFishRemoved => _onFishRemoved;

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

        public bool RegisterAndCheckRecord(FishData fish)
        {
            float fishLength = fish.Length;

            if (!_encyclopedia.ContainsKey(fish))
            {
                _encyclopedia[fish] = 1; 
                _maxRecords[fish] = fishLength; 
                
                Debug.Log($"📖 [도감 최초 등록] 새로운 수종 발견: {fish.Data.fishName} ({fishLength:F1}cm)");
                return true; 
            }

            _encyclopedia[fish]++;
            int currentCatchCount = _encyclopedia[fish];

            float oldRecord = _maxRecords[fish];
            if (fishLength > oldRecord)
            {
                _maxRecords[fish] = fishLength;
                Debug.Log($"🏆 [크기 신기록 경신] {fish.Data.fishName} 기존 {oldRecord:F1}cm -> 신기록 {fishLength:F1}cm");
                return true; 
            }

            return false; 
        }

        public bool TryAddFish(FishData fish)
        {
            if (_storedFish.Count >= _maxCapacity)
            {
                Debug.LogWarning("📦 물고기 보관 상자가 가득 차서 보관할 수 없습니다.");
                return false;
            }

            _storedFish.Add(fish);
            
            _onFishAdded.OnNext(fish); 
            
            return true;
        }
        
        public bool TryRemoveFish(FishData fish)
        {
            if (_storedFish.Remove(fish))
            {
                _onFishRemoved.OnNext(fish);
                return true;
            }
            return false;
        }

        public void UpgradeCapacity(int amount)
        {
            _maxCapacity += amount;
            Debug.Log($"📦 보관 상자가 확장되었습니다. 현재 허용 공간: {_maxCapacity}");
        }

        private void OnDestroy()
        {
            _onFishAdded.Dispose();
            _onFishRemoved.Dispose();
        }
    }
}