using UnityEngine;
using System.Collections.Generic;

namespace FishingSystem.Fishing_Pattern
{
    // 위치와 대기시간을 하나로 묶어주는 구조체 (인스펙터에 노출하기 위해 Serializable 추가)
    [System.Serializable]
    public struct PatternNode
    {
        [Tooltip("이동할 X 좌표")]
        public float targetPositionX;
        
        [Tooltip("해당 좌표 도달 후 대기할 시간(초)")]
        public float waitTime;
    }

    [CreateAssetMenu(fileName = "NewPatternData", menuName = "Fishing System/Pattern Data")]
    public class PatternDataSO : ScriptableObject
    {
        [Header("🐟 물고기 이동 패턴 설정")]
        [Tooltip("물고기가 순차적으로 이동할 좌표와 대기시간 목록입니다.")]
        public List<PatternNode> patternNodes = new List<PatternNode>();

        [Header("반복 설정")]
        [Tooltip("패턴을 끝까지 돌았을 때 처음(0번 인덱스)부터 다시 반복할지 여부")]
        public bool loopPattern = true;
    }
}