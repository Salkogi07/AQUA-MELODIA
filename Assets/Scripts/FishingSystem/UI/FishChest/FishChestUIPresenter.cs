using UnityEngine;
using R3;
using FishingSystem.Data;

namespace FishingSystem.UI
{
    public class FishChestUIPresenter : MonoBehaviour
    {
        [SerializeField] private FishChestUIView view;
        private readonly CompositeDisposable _disposables = new();

        private void Start()
        {
            var manager = FishingDataManager.Instance;
            if (manager == null) return;
            
            // 물고기 추가 스트림 실시간 반영
            manager.OnFishAdded
                .Subscribe(fish =>
                {
                    view.AddFishSlot(fish);
                    view.UpdateCapacityText(manager.StoredFish.Count, manager.MaxCapacity);
                })
                .AddTo(_disposables);

            // 물고기 제거 스트림 실시간 반영
            manager.OnFishRemoved
                .Subscribe(fish =>
                {
                    view.RemoveFishSlot(fish);
                    view.UpdateCapacityText(manager.StoredFish.Count, manager.MaxCapacity);
                })
                .AddTo(_disposables);
        }

        private void OnEnable()
        {
            var manager = FishingDataManager.Instance;
            if (manager != null)
            {
                RefreshAll(manager);
            }
        }

        private void RefreshAll(FishingDataManager manager)
        {
            view.ClearList();
            
            foreach (var fish in manager.StoredFish)
            {
                view.AddFishSlot(fish);
            }

            view.UpdateCapacityText(manager.StoredFish.Count, manager.MaxCapacity);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}