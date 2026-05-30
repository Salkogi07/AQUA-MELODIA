using System.Collections.Generic;
using UnityEngine;

namespace FishingSystem.Fishing_Pattern
{
    [CreateAssetMenu(fileName = "NewEscapePattern", menuName = "FishingSystem/Escape Pattern")]
    public class EscapePatternData : ScriptableObject
    {
        [Header("탈출 경로 정점들 (Local 좌표 기준)")]
        [SerializeField] private List<Vector2> points = new List<Vector2>();
        public IReadOnlyList<Vector2> Points => points;

        [Header("이 패턴 전용 생성 속도 설정")]
        [SerializeField] private int dotsPerFrame = 3;
        public int DotsPerFrame => dotsPerFrame;

        [SerializeField] private int drawDelayMs = 10;
        public int DrawDelayMs => drawDelayMs;
        
        [Header("간격 설정 (Spacing)")]
        [Tooltip("눈에 보이는 비주얼 도트의 간격")]
        [SerializeField] private float dotSpacing = 0.3f;
        public float DotSpacing => dotSpacing;
        
        [Tooltip("보이지 않는 판정선 콜라이더의 간격 (더 작고 촘촘하게 추천)")]
        [SerializeField] private float detectionSpacing = 0.1f;
        public float DetectionSpacing => detectionSpacing;
    }
}