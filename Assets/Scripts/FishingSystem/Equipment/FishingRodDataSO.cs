using UnityEngine;

namespace FishingSystem.Equipment
{
    [CreateAssetMenu(fileName = "NewFishingRod", menuName = "Fishing System/Fishing Rod Equipment")]
    public class FishingRodDataSO : ScriptableObject
    {
        [Header("낚싯대 정보")]
        public string rodName = "기본 낚싯대";
        
        [TextArea]
        [Tooltip("낚싯대에 대한 설명입니다.")]
        public string description = "이 낚싯대에 대한 기본 설명입니다.";

        [Header("💪 낚싯대 기초 스펙")]
        [Tooltip("물고기의 strength 스탯과 비례식으로 매핑할 파워 (기본값: 10)")]
        public float rodPower = 10f;

        [Tooltip("물고기의 agility 스탯을 상쇄하여 물고기의 속도를 낮출 민첩 (기본값: 5)")]
        public float rodAgility = 5f;
        
        [Header("🎮 미니게임 가산 스펙")]
        [Tooltip("물고기 기력을 더 빨리 소진시킬 추가 기력 깎기 보너스 (초당)")]
        public float damageRateBonus = 0f;

        [Tooltip("오차 조준점 허용 범위 보너스 (예: 0.05면 오차 허용 폭 5%p 추가 확장)")]
        public float sweetSpotBonus = 0f;
        
        /// <summary>
        /// 낚싯대의 기본 설명 및 장비 스펙 정보를 서식 문자열로 생성하여 반환합니다.
        /// </summary>
        public string GetFormattedDescription()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // 기본 설명 추가
            sb.AppendLine(description);
            sb.AppendLine();
            
            // 상세 스펙 추가
            sb.AppendLine("<color=#FFA500><b>[장비 장착 효과]</b></color>");
            sb.AppendLine($"• 낚싯대 파워: <color=#FF5555>{rodPower}</color>");
            sb.AppendLine($"• 낚싯대 민첩: <color=#55FF55>{rodAgility}</color>");
            
            if (damageRateBonus > 0f)
            {
                sb.AppendLine($"• 추가 기력 감소: <color=#FFFF55>+{damageRateBonus}/초</color>");
            }
            
            if (sweetSpotBonus > 0f)
            {
                sb.AppendLine($"• 조준 허용 범위 확장: <color=#55FFFF>+{sweetSpotBonus * 100f:F0}%p</color>");
            }
            
            return sb.ToString();
        }
    }
}