using System.Collections.Generic;
using UnityEngine;

namespace FishingSystem.Pattern
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
        
        [SerializeField] private float dotSpacing = 0.3f;
        public float DotSpacing => dotSpacing;
    }
}