using System.Collections.Generic;
using UnityEngine;

namespace FishingSystem.Fishing_Pattern
{
    [System.Serializable]
    public class PatternNode
    {
        [Tooltip("목표 X 좌표")]
        public float targetPositionX;
        
        [Tooltip("해당 위치까지 이동하는 데 걸리는 시간 (초)")]
        public float moveDuration = 1.5f;

        [Tooltip("도착 후 머무르는 대기 시간 (초)")]
        public float waitTime = 0.5f;
    }

    [CreateAssetMenu(fileName = "New Fish Pattern", menuName = "Fishing System/Pattern Data")]
    public class PatternDataSO : ScriptableObject
    {
        [Tooltip("패턴이 끝나면 처음부터 다시 반복할지 여부")]
        public bool loopPattern = true;
        
        public List<PatternNode> patternNodes = new List<PatternNode>();
    }
}