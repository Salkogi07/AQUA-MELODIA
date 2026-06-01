using FishingSystem.Fishing_Pattern;
using UnityEngine;

namespace FishingSystem.Fish
{
    [CreateAssetMenu(fileName = "NewFishData", menuName = "Fishing System/Fish Data")]
    public class FishDataSO : ScriptableObject
    {
        [Header("물고기 기본 정보")]
        public string fishName = "이름 미정";
        public FishGrade grade = FishGrade.Common;

        [Header("스크립터블 데이터")]
        [Tooltip("물고기가 잡히기 까지의 체력")]
        public float maxStamina = 100f;    
        
        [Tooltip("낚싯대의 힘보다 강할 경우 기력 감소 효과 적용")]
        public float strength = 10f;       
        
        [Tooltip("물고기가 어느정도로 움직일지 설정")]
        public float resistance = 5f;     
        
        [Tooltip("물고기가 얼마나 빨리 이동 할지 설정")]
        public float agility = 3f;         

        [Header("패턴 데이터")]
        [Tooltip("추후 구현될 미니게임 패턴 데이터 구조체나 리스트가 들어갈 자리")]
        public EscapePatternDataSO patternData; 
    }
}