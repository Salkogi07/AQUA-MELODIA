using UnityEngine;
using System.Collections.Generic;

namespace FishingSystem.Fish
{
    public class FishingZone : MonoBehaviour
    {
        [Header("낚시터 설정")]
        public string zoneName = "일반 호수";

        [Header("이 구역에서 출현하는 물고기 목록")]
        [SerializeField] private List<FishDataSO> availableFishList = new();

        /// <summary>
        /// 해당 구역의 물고기 풀에서 한 마리를 무작위로 추첨합니다.
        /// </summary>
        public FishDataSO GetRandomFish()
        {
            if (availableFishList == null || availableFishList.Count == 0)
            {
                Debug.LogWarning($"⚠️ [{zoneName}] 구역에 등록된 물고기 데이터가 없습니다!");
                return null;
            }
            return availableFishList[Random.Range(0, availableFishList.Count)];
        }
    }
}