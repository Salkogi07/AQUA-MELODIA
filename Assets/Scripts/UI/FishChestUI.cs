using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FishingSystem.Data;
using R3;

namespace FishingSystem.UI
{
    public class FishChestUI : MonoBehaviour
    {
        [Header("목록 표시용 프리팹 템플릿")]
        [Tooltip("FishSlotUI 스크립트가 최상단 컴포넌트로 조립된 슬롯 프리팹이어야 합니다.")]
        [SerializeField] private GameObject fishSlotPrefab;
        [SerializeField] private Transform listContainer;

        [Header("용량 표시")]
        [SerializeField] private Text capacityText;

        [Header("연결할 툴팁 UI")]
        [SerializeField] private FishTooltipUI tooltipUI;

        private readonly CompositeDisposable _disposables = new();

        private void OnEnable()
        {
            var manager = FishingDataManager.Instance;
            if (manager == null) return;

            // 1. 활성화 시점에 이전 렌더링 내역 최신 정보로 한 번 맞추기
            RefreshStoredFishList();

            // 2. 실시간 반응형 대응 구독 시작
            manager.OnFishAdded
                .Subscribe(fish =>
                {
                    AddFishSlot(fish);
                    UpdateCapacity(manager);
                })
                .AddTo(_disposables);

            manager.OnFishRemoved
                .Subscribe(fish =>
                {
                    RemoveFishSlot(fish);
                    UpdateCapacity(manager);
                })
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.Clear();
        }

        public void RefreshStoredFishList()
        {
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }

            var manager = FishingDataManager.Instance;
            if (manager == null) return;

            foreach (var fish in manager.StoredFish)
            {
                AddFishSlot(fish);
            }

            UpdateCapacity(manager);
        }

        private void AddFishSlot(Fish.FishData fish)
        {
            if (fish == null) return;

            GameObject slotObj = Instantiate(fishSlotPrefab, listContainer);
            if (slotObj.TryGetComponent<FishSlotUI>(out var slotUI))
            {
                slotUI.SetupSlot(fish, tooltipUI);
            }
            else
            {
                Destroy(slotObj);
                Debug.LogWarning($"⚠️ 생성된 슬롯 프리팹({slotObj.name})에 'FishSlotUI' 스크립트가 없습니다.");
            }
        }

        private void RemoveFishSlot(Fish.FishData fish)
        {
            if (fish == null) return;

            foreach (Transform child in listContainer)
            {
                if (child.TryGetComponent<FishSlotUI>(out var slotUI))
                {
                    // 메모리상 동일 인스턴스인 경우 제거 타겟으로 지정
                    if (object.ReferenceEquals(slotUI.CurrentFishData, fish))
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }

        private void UpdateCapacity(FishingDataManager manager)
        {
            if (capacityText != null)
            {
                capacityText.text = $"{manager.StoredFish.Count} / {manager.MaxCapacity}";
            }
        }
    }
}