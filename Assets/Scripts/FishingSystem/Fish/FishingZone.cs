using System;
using UnityEngine;
using System.Collections.Generic;
using FishingSystem.Bait;
using FishingSystem.Data;
using R3;
using Random = UnityEngine.Random;

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
        public Stat weight = new Stat(); 
        [HideInInspector] public float calculatedChance; 
    }

    public class FishingZone : MonoBehaviour
    {
        [Header("🔍 디버그 및 확률 모니터링 설정")]
        public bool enableDebugLog = true;
        
        public string zoneName = "새 낚시터";
        public FishingRegion zoneRegion = FishingRegion.Ocean; 
        public float maxMoveRange = 5f;

        public List<GradeChance> gradeChances = new();
        public List<FishSpawnEntry> fishSpawnList = new();

        private void Awake()
        {
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
            // 우회 추가된 CurrentBait 프로퍼티를 활용하여 안전하게 수입
            BaitDataSO activeBait = BaitManager.Instance != null ? BaitManager.Instance.CurrentBait : null;

            if (enableDebugLog)
            {
                string baitName = activeBait != null ? activeBait.baitName : "없음 (미장착)";
                Debug.Log($"<color=cyan>🎣 [낚시터 추첨 시작]</color> 지역: <b>{zoneName} ({zoneRegion})</b> | 장착 미끼: <b>{baitName}</b>");
            }

            // 1. 미끼 사양에 맞춘 임시 등급 확률 테이블 계산 복사
            List<GradeChance> tempGradeChances = CalculateDynamicGradeChances(activeBait);

            // 2. 등급 주사위 굴리기 (미끼 보정 확률 적용)
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;
            FishGrade selectedGrade = FishGrade.Common;

            foreach (var gc in tempGradeChances)
            {
                cumulative += gc.probability;
                if (roll <= cumulative) 
                { 
                    selectedGrade = gc.grade; 
                    break; 
                }
            }

            if (enableDebugLog)
            {
                Debug.Log($"<color=yellow>🎲 [등급 추첨 결과]</color> 주사위 값: <b>{roll:F2}</b> (선택된 등급: <b>{selectedGrade}</b>)");
            }

            var candidates = fishSpawnList.FindAll(f => f.fishData != null && f.fishData.grade == selectedGrade);
            if (candidates.Count == 0)
            {
                if (enableDebugLog) Debug.LogWarning($"⚠️ [경고] {selectedGrade} 등급에 해당하는 물고기 후보가 없습니다!");
                return null;
            }

            // 3. 내부 가중치 스탯 보정 적용 및 최종 물고기 선별
            int totalWeight = 0;
            Dictionary<FishSpawnEntry, int> modifiedWeightCache = new();

            if (enableDebugLog)
            {
                Debug.Log($"<color=orange>⚖️ [{selectedGrade} 등급 후보군 가중치 계산]</color>");
            }

            foreach (var entry in candidates)
            {
                int modifiedWeight = EvaluateBaitWeightInfluence(entry, activeBait);
                modifiedWeightCache[entry] = modifiedWeight;
                totalWeight += modifiedWeight;

                if (enableDebugLog)
                {
                    Debug.Log($" - 후보: <b>{entry.fishData.fishName}</b> | 최종 가중치: <b>{modifiedWeight}</b>");
                }
            }

            if (totalWeight <= 0) return candidates[0].fishData;

            int weightRoll = Random.Range(0, totalWeight);
            int weightCumulative = 0;
            FishDataSO selectedFish = null;

            foreach (var entry in candidates)
            {
                weightCumulative += modifiedWeightCache[entry];
                if (weightRoll < weightCumulative)
                {
                    selectedFish = entry.fishData;
                    break;
                }
            }

            if (selectedFish == null) selectedFish = candidates[0].fishData;

            if (enableDebugLog)
            {
                Debug.Log($"<color=green>🏆 [최종 물고기 당첨]</color> 가중치 총합: {totalWeight} | 추첨 값: {weightRoll} | 당첨 물고기: <b>{selectedFish.fishName}</b>");
            }

            // 4. 적용했던 스탯 원복 처리
            foreach (var entry in candidates)
            {
                RevertBaitWeightInfluence(entry, activeBait);
            }

            return selectedFish;
        }

        /// <summary>
        /// 미끼 성능을 검사하여 실시간 등급 확률 가산치를 적용하고 100% 합계를 재산출합니다.
        /// </summary>
        private List<GradeChance> CalculateDynamicGradeChances(BaitDataSO bait)
        {
            List<GradeChance> adjustedChances = new();
            foreach (var gc in gradeChances)
            {
                adjustedChances.Add(new GradeChance { grade = gc.grade, probability = gc.probability });
            }

            if (bait == null) return adjustedChances;

            float totalBoostAmount = 0f;
            string boostDetails = "";

            // [조건 1] 특정 저격 물고기 미끼에 의한 등급 확률 향상
            foreach (var pref in bait.preferredFishList)
            {
                if (pref.targetFish != null && pref.gradeChanceBoost > 0f)
                {
                    float added = AddGradeProbability(adjustedChances, pref.targetFish.grade, pref.gradeChanceBoost);
                    totalBoostAmount += added;
                    boostDetails += $"\n   - [저격보너스] {pref.targetFish.name} ({pref.targetFish.grade}): +{added}%";
                }
            }

            // [조건 2] 지역 기반 특정 등급 미끼에 의한 등급 확률 향상
            foreach (var bonus in bait.regionGradeBonusList)
            {
                if (bonus.targetRegion == this.zoneRegion && bonus.gradeChanceBoost > 0f)
                {
                    float added = AddGradeProbability(adjustedChances, bonus.targetGrade, bonus.gradeChanceBoost);
                    totalBoostAmount += added;
                    boostDetails += $"\n   - [지역보너스] {bonus.targetRegion} / {bonus.targetGrade}: +{added}%";
                }
            }

            // [정규화] 상승한 보너스 확률 합계만큼 하위 등급(Common 등)에서 감산 처리하여 전체 확률 100% 균형을 맞춤
            if (totalBoostAmount > 0f)
            {
                NormalizeChances(adjustedChances, totalBoostAmount);
                
                if (enableDebugLog)
                {
                    Debug.Log($"<color=magenta>📈 [미끼 등급 확률 보정 내역]</color>{boostDetails}\n   - 총 상승 보정치: +{totalBoostAmount}% (하위 등급 자동 차감 완료)");
                }
            }

            return adjustedChances;
        }

        private float AddGradeProbability(List<GradeChance> list, FishGrade targetGrade, float amount)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].grade == targetGrade)
                {
                    var updated = list[i];
                    updated.probability += amount;
                    list[i] = updated;
                    return amount;
                }
            }
            return 0f;
        }

        private void NormalizeChances(List<GradeChance> list, float deductAmount)
        {
            float remainingToDeduct = deductAmount;
            
            // 확률을 깎아낼 대상 순서 (Common -> Rare -> Epic 순으로 차감하며 보정)
            FishGrade[] orderOfReduction = { FishGrade.Common, FishGrade.Rare, FishGrade.Epic };

            foreach (var grade in orderOfReduction)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].grade == grade)
                    {
                        float currentProb = list[i].probability;
                        float deduction = Mathf.Min(currentProb, remainingToDeduct);
                        
                        var updated = list[i];
                        updated.probability -= deduction;
                        list[i] = updated;

                        remainingToDeduct -= deduction;
                        if (remainingToDeduct <= 0f) return;
                    }
                }
            }
        }

        private int EvaluateBaitWeightInfluence(FishSpawnEntry entry, BaitDataSO bait)
        {
            // Value 속성 대신 확실하게 검증된 GetValue() 호출로 대체[cite: 1, 3]
            if (bait == null) return entry.weight.GetValue();

            foreach (var pref in bait.preferredFishList)
            {
                if (pref.targetFish == entry.fishData && pref.weightBonus != 0)
                {
                    entry.weight.AddModifier(pref.weightBonus);
                }
            }

            return Mathf.Max(0, entry.weight.GetValue());
        }

        private void RevertBaitWeightInfluence(FishSpawnEntry entry, BaitDataSO bait)
        {
            if (bait == null) return;

            foreach (var pref in bait.preferredFishList)
            {
                if (pref.targetFish == entry.fishData && pref.weightBonus != 0)
                {
                    entry.weight.RemoveModifier(pref.weightBonus);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.75f);
            Vector3 centerPos = transform.position;
            
            Vector3 leftLimit = centerPos + new Vector3(-maxMoveRange, 0, 0);
            Vector3 rightLimit = centerPos + new Vector3(maxMoveRange, 0, 0);
            
            Gizmos.DrawLine(leftLimit, rightLimit);

            const float limitTickHeight = 0.5f;
            
            Gizmos.DrawLine(leftLimit + new Vector3(0, -limitTickHeight, 0), leftLimit + new Vector3(0, limitTickHeight, 0));
            Gizmos.DrawLine(rightLimit + new Vector3(0, -limitTickHeight, 0), rightLimit + new Vector3(0, limitTickHeight, 0));
        }
    }
}