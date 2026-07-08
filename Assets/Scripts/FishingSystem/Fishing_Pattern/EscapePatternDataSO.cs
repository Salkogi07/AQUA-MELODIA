using System.Collections.Generic;
using UnityEngine;

namespace FishingSystem.Fishing_Pattern
{
    [CreateAssetMenu(fileName = "NewEscapePattern", menuName = "Fishing System/Escape Pattern")]
    public class EscapePatternDataSO : ScriptableObject
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
        
        [Header("미니게임 설정")]
        [Tooltip("선을 그릴 수 있는 제한 시간 (초)")]
        [SerializeField] private float timeLimit = 3f;
        public float TimeLimit => timeLimit;

        [Tooltip("전체 패턴 길이 대비 제공할 잉크량의 배수 (1.2면 20% 여유 제공)")]
        [SerializeField] private float inkBufferMultiplier = 1.2f;
        public float InkBufferMultiplier => inkBufferMultiplier;
    }
}