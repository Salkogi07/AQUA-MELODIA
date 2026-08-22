using UnityEngine;
using System.Collections.Generic;
using FishingSystem.Fish;

namespace FishingSystem.Bait
{
    [System.Serializable]
    public struct FishBaitBonus
    {
        [Tooltip("가중치 및 등급 확률이 상승할 대상 물고기 수종")]
        public FishDataSO targetFish;
        
        [Range(0f, 100f)]
        [Tooltip("해당 물고기가 속한 등급의 전체 출현 확률 증가치 (%) - 예: 5 입력 시 전설 등급 자체의 출현율이 +5%")]
        public float gradeChanceBoost;

        [Tooltip("해당 등급 내에서 이 물고기가 최종 선택될 세부 가중치 보너스 스탯 값")]
        public int weightBonus;
    }

    [System.Serializable]
    public struct RegionGradeBaitBonus
    {
        [Tooltip("대상 지역 환경")]
        public FishingRegion targetRegion;
        [Tooltip("확률이 상승할 대상 물고기 등급")]
        public FishGrade targetGrade;

        [Range(0f, 100f)]
        [Tooltip("이 지역에서 해당 등급 자체가 출현할 확률 증가치 (%)")]
        public float gradeChanceBoost;
    }

    [CreateAssetMenu(fileName = "NewBait", menuName = "Fishing System/Bait Data")]
    public class BaitDataSO : ScriptableObject
    {
        public string baitName = "새로운 미끼";
        public Sprite baitSprite;
        [TextArea] public string description = "미끼에 대한 설명입니다.";

        [Header("💡 [유형 1] 특정 물고기 저격 (등급 출현율 % + 내부 가중치 스탯 동시 보정)")]
        public List<FishBaitBonus> preferredFishList = new();

        [Header("💡 [유형 2] 지역 전용 등급 부스터 (특정 지역 등급 출현율 % + 내부 가중치 스탯 동시 보정)")]
        public List<RegionGradeBaitBonus> regionGradeBonusList = new();
        
        /// <summary>
        /// 미끼의 기본 설명 및 동적 보너스 수치를 서식 문자열로 생성하여 반환합니다.
        /// </summary>
        public string GetFormattedDescription()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // 기본 설명 추가
            sb.AppendLine(description);
            sb.AppendLine();
            
            // 보너스 효과 추가
            sb.AppendLine("<color=#FFA500><b>[미끼 사용 효과]</b></color>");

            bool hasEffects = false;

            // 1. 특정 물고기 저격 보너스 정보 검사
            if (preferredFishList != null && preferredFishList.Count > 0)
            {
                bool preferredSectionAdded = false;
                
                foreach (var pref in preferredFishList)
                {
                    if (pref.targetFish == null) continue;
                    
                    if (!preferredSectionAdded)
                    {
                        sb.AppendLine("<b><color=#88FF88>• 특정 수종 선호 효과:</color></b>");
                        preferredSectionAdded = true;
                        hasEffects = true;
                    }

                    string fishName = pref.targetFish.fishName;
                    string bonusText = "";

                    if (pref.gradeChanceBoost > 0f)
                        bonusText += $"해당 등급 출현율 <color=#FFFF55>+{pref.gradeChanceBoost}%</color> ";
                    
                    if (pref.weightBonus != 0)
                        bonusText += $"어종 개별 가중치 <color=#55FFFF>+{pref.weightBonus}</color>";

                    sb.AppendLine($"  - {fishName} : {bonusText}");
                }
            }

            // 2. 지역 전용 등급 부스터 보너스 정보 검사
            if (regionGradeBonusList != null && regionGradeBonusList.Count > 0)
            {
                if (hasEffects) sb.AppendLine(); // 한 줄 줄바꿈 추가
                
                bool regionSectionAdded = false;

                foreach (var bonus in regionGradeBonusList)
                {
                    if (bonus.gradeChanceBoost <= 0f) continue;

                    if (!regionSectionAdded)
                    {
                        sb.AppendLine("<b><color=#88CCFF>• 지역별 환경 확률 보정:</color></b>");
                        regionSectionAdded = true;
                        hasEffects = true;
                    }

                    string regionStr = ConvertRegionToString(bonus.targetRegion);
                    string gradeStr = ConvertGradeToString(bonus.targetGrade);

                    sb.AppendLine($"  - [{regionStr}] 구역에서 <b>{gradeStr}</b> 등급 확률 <color=#FFFF55>+{bonus.gradeChanceBoost}%</color>");
                }
            }

            if (!hasEffects)
            {
                sb.AppendLine("• 특별한 확률 변경 보너스 효과가 없습니다.");
            }

            return sb.ToString();
        }

        private string ConvertRegionToString(FishingRegion region)
        {
            return region switch
            {
                FishingRegion.Ocean => "바다",
                FishingRegion.River => "강",
                FishingRegion.Lake => "호수",
                FishingRegion.DeepSea => "심해",
                _ => region.ToString()
            };
        }

        private string ConvertGradeToString(FishGrade grade)
        {
            return grade switch
            {
                FishGrade.Common => "일반",
                FishGrade.Rare => "희귀",
                FishGrade.Epic => "에픽",
                FishGrade.Unique => "유니크",
                FishGrade.Legend => "전설",
                _ => grade.ToString()
            };
        }
    }
}