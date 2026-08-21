using FishingSystem.Fishing_Pattern;
using UnityEngine;

namespace FishingSystem.Fish
{
    [CreateAssetMenu(fileName = "NewFishData", menuName = "Fishing System/Fish Data")]
    public class FishDataSO : ScriptableObject
    {
        [Header("물고기 기본 정보")]
        public FishGrade grade = FishGrade.Common;
        public FishingRegion habitatRegion = FishingRegion.Ocean;
        public string fishName = "이름 미정";
        public Sprite fishSprite;
        public GameObject fishPrefab;
        
        [Header("📏 생체 크기 범위")]
        [Tooltip("물고기가 생성될 수 있는 최소 길이(cm)")]
        public float minLength = 10f;
        [Tooltip("물고기가 생성될 수 있는 최대 길이(cm)")]
        public float maxLength = 120f;

        [Header("스크립터블 데이터")]
        [Tooltip("물고기가 잡히기 까지의 체력")]
        public float maxStamina = 100f;
        [Tooltip("낚싯대의 힘보다 강할 경우 기력 감소 효과 적용")]
        public float strength = 10f;
        [Tooltip("물고기가 얼마나 빨리 이동 할지 설정")]
        public float agility = 3f;
        
        [Header("패턴 데이터")]
        [Tooltip("미니게임 진행 시 물고기의 움직임 패턴")]
        public PatternDataSO patternData;
        [Tooltip("물고기 발악 패턴")]
        public EscapePatternDataSO escapePatternData; 
    }
}