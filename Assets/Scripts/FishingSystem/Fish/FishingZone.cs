using UnityEngine;
using System.Collections.Generic;

namespace FishingSystem.Fish
{
    [System.Serializable]
    public struct GradeChance
    {
        public FishGrade grade;
        public float probability;
    }

    [System.Serializable]
    public class FishSpawnEntry
    {
        public FishDataSO fishData;
        public int weight = 10;
        [HideInInspector] public float calculatedChance; // 에디터 표시용
    }

    public class FishingZone : MonoBehaviour
    {
        public string zoneName = "새 낚시터";
        public float maxMoveRange = 5f;

        // 에디터에서 접근할 리스트
        public List<GradeChance> gradeChances = new();
        public List<FishSpawnEntry> fishSpawnList = new();

        private void Awake()
        {
            // 게임 시작 시 등급 리스트가 비어있다면 초기화
            if (gradeChances.Count == 0)
            {
                foreach (FishGrade grade in System.Enum.GetValues(typeof(FishGrade)))
                {
                    gradeChances.Add(new GradeChance { grade = grade, probability = (grade == FishGrade.Common ? 100f : 0f) });
                }
            }
        }

        public FishDataSO GetRandomFish()
        {
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;
            FishGrade selectedGrade = FishGrade.Common;

            foreach (var gc in gradeChances)
            {
                cumulative += gc.probability;
                if (roll <= cumulative) { selectedGrade = gc.grade; break; }
            }

            var candidates = fishSpawnList.FindAll(f => f.fishData != null && f.fishData.grade == selectedGrade);
            if (candidates.Count == 0) return null;

            int totalWeight = 0;
            foreach (var c in candidates) totalWeight += c.weight;
            
            int weightRoll = Random.Range(0, totalWeight);
            int weightCumulative = 0;
            foreach (var entry in candidates)
            {
                weightCumulative += entry.weight;
                if (weightRoll < weightCumulative) return entry.fishData;
            }
            return candidates[0].fishData;
        }
    }
}