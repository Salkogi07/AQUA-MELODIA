using UnityEngine;
using System.Collections.Generic;

namespace FishingSystem.Fish
{
    public class FishingZone : MonoBehaviour
    {
        [Header("낚시터 설정")]
        public string zoneName = "일반 호수";
        
        [Header("미니게임 범위 설정")]
        [Tooltip("물고기가 좌우로 움직일 수 있는 최대 반경 (0을 기준으로 양옆)")]
        public float maxMoveRange = 5f;

        [Header("이 구역에서 출현하는 물고기 목록")]
        [SerializeField] private List<FishDataSO> availableFishList = new();

        /// <summary>
        /// 해당 구역의 물고기 풀에서 한 마리를 무작위로 추첨합니다.
        /// </summary>
        public FishDataSO GetRandomFish()
        {
            // 빈칸(null)으로 되어있는 데이터를 걸러냅니다.
            List<FishDataSO> validFish = availableFishList.FindAll(f => f != null);

            if (validFish.Count == 0)
            {
                Debug.LogWarning($"⚠️ [{zoneName}] 구역에 유효한 물고기 데이터가 없습니다!");
                return null;
            }
            return validFish[Random.Range(0, validFish.Count)];
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            Vector3 center = transform.position;
            Vector3 leftLimit = center + Vector3.left * maxMoveRange;
            Vector3 rightLimit = center + Vector3.right * maxMoveRange;

            // 중앙선 (기준점)
            Gizmos.DrawLine(center + Vector3.up * 0.5f, center + Vector3.down * 0.5f);
            
            // 범위를 나타내는 가로 선 (좌측 끝 ~ 우측 끝)
            Gizmos.DrawLine(leftLimit, rightLimit);

            // 좌측 끝 표시(수직선)
            Gizmos.DrawLine(leftLimit + Vector3.up * 0.5f, leftLimit + Vector3.down * 0.5f);
            
            // 우측 끝 표시(수직선)
            Gizmos.DrawLine(rightLimit + Vector3.up * 0.5f, rightLimit + Vector3.down * 0.5f);
        }

        #if UNITY_EDITOR
        // 에디터(인스펙터)에서 값이 바뀔 때마다 자동으로 실행되는 검증 로직
        private void OnValidate()
        {
            if (availableFishList != null)
            {
                for (int i = 0; i < availableFishList.Count; i++)
                {
                    FishDataSO fish = availableFishList[i];
                    
                    // 패턴 데이터가 연결되어 있다면 검사
                    if (fish != null && fish.patternData != null && fish.patternData.patternNodes != null)
                    {
                        bool isExceeding = false;
                        
                        // 물고기가 움직이는 노드 중 하나라도 낚시터 범위를 벗어나는지 확인
                        foreach (var node in fish.patternData.patternNodes)
                        {
                            if (Mathf.Abs(node.targetPositionX) > maxMoveRange)
                            {
                                isExceeding = true;
                                break;
                            }
                        }

                        if (isExceeding)
                        {
                            Debug.LogWarning($"<color=orange>[{zoneName}] 경고: <b>'{fish.fishName}'</b> 물고기의 움직임 패턴이 낚시터 최대 크기({maxMoveRange})를 벗어납니다! 목록에서 제외(Null) 처리됩니다.</color>");
                            // 목록에서 즉시 비워버려서 참조가 안 되도록 강제 처리
                            availableFishList[i] = null; 
                        }
                    }
                }
            }
        }
        #endif
    }
}