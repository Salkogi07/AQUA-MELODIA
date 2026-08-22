using System.Collections.Generic;
using UnityEngine;
using FishingSystem.Fish;

namespace FishingSystem.House
{
    public class Aquarium : MonoBehaviour
    {
        [Header("어항 영역 설정")]
        [Tooltip("물고기가 헤엄칠 수 있는 영역을 결정하는 BoxCollider2D (Trigger 체크 필수)")]
        [SerializeField] private BoxCollider2D swimArea;

        [Header("수영 물리 스펙")]
        public float minSwimSpeed = 0.5f;
        public float maxSwimSpeed = 1.8f;
        [Tooltip("목적지 도달 시 최대 대기시간")]
        public float directionChangeDelay = 2.0f;

        [Header("스프라이트 머리 방향 설정")]
        [Tooltip("물고기 프리팹 원본 이미지의 머리가 '오른쪽'을 향하고 있다면 체크하세요. '왼쪽'을 향하고 있다면 체크를 해제하세요.")]
        public bool originalSpriteFacesRight = false;

        private readonly List<GameObject> _spawnedFishes = new();

        private void Start()
        {
            if (swimArea == null)
            {
                swimArea = GetComponent<BoxCollider2D>();
            }

            SpawnFishesFromHouseData();
        }

        public void SpawnFishesFromHouseData()
        {
            ClearAquarium();

            if (HouseDataManager.Instance == null)
            {
                Debug.LogWarning("⚠️ HouseDataManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            var fishList = HouseDataManager.Instance.HousedFish;
            foreach (var fish in fishList)
            {
                if (fish == null || fish.Data == null) continue;

                GameObject prefabToSpawn = fish.Data.fishPrefab;
                if (prefabToSpawn == null)
                {
                    Debug.LogWarning($"⚠️ '{fish.Data.fishName}' 어종에 등록된 프리팹이 존재하지 않습니다.");
                    continue;
                }

                Vector3 spawnPos = GetRandomPointInBounds();

                GameObject spawned = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
                _spawnedFishes.Add(spawned);

                // 스폰할 때 프리팹 원본의 머리 방향 옵션을 넘겨줍니다.
                AquariumFish swimBehavior = spawned.AddComponent<AquariumFish>();
                swimBehavior.Initialize(swimArea, minSwimSpeed, maxSwimSpeed, directionChangeDelay, originalSpriteFacesRight);
            }
        }

        private void ClearAquarium()
        {
            foreach (var fish in _spawnedFishes)
            {
                if (fish != null) Destroy(fish);
            }
            _spawnedFishes.Clear();
        }

        private Vector3 GetRandomPointInBounds()
        {
            if (swimArea == null) return transform.position;

            Bounds bounds = swimArea.bounds;
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            float z = transform.position.z;

            return new Vector3(x, y, z);
        }
    }
}