using System.Collections.Generic;
using UnityEngine;
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

        // [도감 사전 (Key: FishData 개체, Value: 포획 누적 횟수)
        private readonly Dictionary<FishData, int> _encyclopedia = new();
        public IReadOnlyDictionary<FishData, int> Encyclopedia => _encyclopedia;

        // 어종별 신기록 최대 길이 보관 (Key: FishData 개체, Value: 최대 기록 길이)
        private readonly Dictionary<FishData, float> _maxRecords = new();

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
        /// 도감 누적 등록 및 신기록(Max Length) 갱신 여부를 가늠합니다.
        /// </summary>
        public bool RegisterAndCheckRecord(FishData fish)
        {
            float fishLength = fish.Length;

            // 1. 도감 최초 등록 여부 확인
            // (FishData 내부의 Equals가 작동하여 동일 어종을 자동 판별합니다)
            if (!_encyclopedia.ContainsKey(fish))
            {
                _encyclopedia[fish] = 1; // 누적 1회 등록
                _maxRecords[fish] = fishLength; // 최초 크기 기록 등록
                
                Debug.Log($"📖 [도감 최초 등록] 새로운 수종 발견: {fish.Data.fishName} ({fishLength:F1}cm) / 포획 횟수: 1회");
                return true; // 무조건 자랑하기 연출 트리거
            }

            // 2. 이미 존재하는 어종인 경우 누적 횟수 증가
            _encyclopedia[fish]++;
            int currentCatchCount = _encyclopedia[fish];
            Debug.Log($"📊 [도감 업데이트] 어종: {fish.Data.fishName} | 누적 포획 횟수: {currentCatchCount}회");

            // 3. 기록 측정 및 갱신 판단
            float oldRecord = _maxRecords[fish];
            if (fishLength > oldRecord)
            {
                _maxRecords[fish] = fishLength;
                Debug.Log($"🏆 [크기 신기록 경신] {fish.Data.fishName} 기존 {oldRecord:F1}cm -> 신기록 {fishLength:F1}cm");
                return true; // 신기록 수립 시 자랑하기 트리거
            }

            return false; // 신기록 실패 시 자랑하기 바이패스
        }

        public bool TryAddFish(FishData fish)
        {
            if (_storedFish.Count >= _maxCapacity)
            {
                Debug.LogWarning("📦 물고기 보관 상자가 가득 차서 보관할 수 없습니다.");
                return false;
            }

            _storedFish.Add(fish);
            return true;
        }

        public void UpgradeCapacity(int amount)
        {
            _maxCapacity += amount;
            Debug.Log($"📦 보관 상자가 확장되었습니다. 현재 허용 공간: {_maxCapacity}");
        }
    }
}