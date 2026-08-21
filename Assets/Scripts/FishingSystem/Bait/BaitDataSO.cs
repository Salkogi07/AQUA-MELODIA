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
    }
}