using System.Collections.Generic;
using FishingSystem.Fish;
using UnityEngine;

namespace FishingSystem.Island
{
    [System.Serializable]
    public struct IslandFeature
    {
        [Tooltip("기능 분류 이름 (예: Marina, Market, DeepSea 등)")]
        public string featureName;

        [Tooltip("실제 UI 버튼에 표시될 이름 (예: 선착장 가기, 현지 마켓 입장)")]
        public string buttonName;
        
        [Tooltip("이동할 대상 씬 이름")]
        public string sceneName;
    }

    [CreateAssetMenu(fileName = "NewIslandData", menuName = "Fishing System/Island Data")]
    public class IslandDataSO : ScriptableObject
    {
        [Header("🏝️ 섬 기본 정보")]
        public string islandId;          // 고유 식별자
        public string islandName;        // 화면에 표시될 섬 이름
        [TextArea(3, 5)]
        public string islandDescription; // 섬 설명

        [Header("🔓 해금 조건")]
        [Tooltip("해금에 필요한 게임 재화(골드)")]
        public int requiredGold;
        
        [Tooltip("해금하기 전에 반드시 한 번 이상 낚아야 하는 대상 어종 목록")]
        public List<FishDataSO> requiredFishList = new();

        [Header("🚪 이동 가능한 구역/기능 씬")]
        [Tooltip("이 섬에서 로드할 수 있는 씬과 해당 기능 목록")]
        public List<IslandFeature> features = new();
    }
}