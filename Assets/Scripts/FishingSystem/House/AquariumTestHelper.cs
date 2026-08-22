using System.Collections.Generic;
using UnityEngine;
using FishingSystem.Fish;

namespace FishingSystem.House
{
    public class AquariumTestHelper : MonoBehaviour
    {
        [Header("테스트 데이터 주입 설정")]
        [Tooltip("어항에 테스트로 스폰시킬 물고기 데이터(SO)들을 여기에 등록하세요.")]
        [SerializeField] private List<FishDataSO> testFishDataList = new List<FishDataSO>();

        [Header("연결할 어항")]
        [Tooltip("비워둘 경우 씬 내부의 Aquarium 컴포넌트를 자동으로 탐색하여 적용합니다.")]
        [SerializeField] private Aquarium targetAquarium;

        private void Start()
        {
            if (targetAquarium == null)
            {
                targetAquarium = FindFirstObjectByType<Aquarium>();
            }

            if (HouseDataManager.Instance == null)
            {
                Debug.LogError("⚠️ 씬 내에 HouseDataManager(싱글톤)가 존재하지 않습니다! 먼저 씬에 매니저 오브젝트를 배치해 주세요.");
                return;
            }

            // 1. 임의의 테스트 데이터를 하우스 싱글톤 데이터베이스에 주입
            InjectDummyData();

            // 2. 어항을 강제로 갱신하여 주입된 데이터를 바탕으로 즉시 스폰
            if (targetAquarium != null)
            {
                targetAquarium.SpawnFishesFromHouseData();
            }
            else
            {
                Debug.LogWarning("⚠️ 씬 내부에서 Aquarium 컴포넌트를 찾을 수 없어 스폰을 생략합니다.");
            }
        }

        private void InjectDummyData()
        {
            if (testFishDataList == null || testFishDataList.Count == 0)
            {
                Debug.LogWarning("⚠️ 테스트용 물고기 SO 리스트가 비어있습니다. 인스펙터에서 임의의 FishDataSO 데이터를 등록해 주세요.");
                return;
            }

            foreach (var fishSO in testFishDataList)
            {
                if (fishSO == null) continue;

                // 실시간 보관 형식인 FishData로 변환 및 생성
                FishData dummyFish = new FishData(fishSO);

                // 하우스 매니저에 강제 등록
                HouseDataManager.Instance.TryAddFishToHouse(dummyFish);
            }

            Debug.Log($"🧪 [임의 데이터 주입 성공] 총 {testFishDataList.Count}마리의 테스트 물고기가 어항 데이터에 임시 주입되었습니다.");
        }
    }
}