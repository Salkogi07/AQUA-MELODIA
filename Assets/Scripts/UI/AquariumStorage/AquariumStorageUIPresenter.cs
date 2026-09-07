using UnityEngine;
using R3;
using FishingSystem.Data;
using FishingSystem.House;

namespace UI.AquariumStorage
{
    public class AquariumStorageUIPresenter : MonoBehaviour
    {
        [SerializeField] private AquariumStorageUIView view;
        [SerializeField] private Aquarium targetAquarium;

        private readonly CompositeDisposable _disposables = new();

        private void Start()
        {
            var dm = FishingDataManager.Instance;
            if (dm == null) return;

            // 보관함 데이터가 변경될 때마다 리스트 갱신
            dm.OnFishAdded.Subscribe(_ => RefreshAll()).AddTo(_disposables);
            dm.OnFishRemoved.Subscribe(_ => RefreshAll()).AddTo(_disposables);

            RefreshAll();
        }

        private void OnEnable() => RefreshAll();

        private void RefreshAll()
        {
            var dm = FishingDataManager.Instance;
            if (dm == null || view == null) return;

            view.ClearList();
            foreach (var fish in dm.StoredFish)
            {
                view.AddFishSlot(fish, targetAquarium);
            }
            view.UpdateCapacityText(dm.StoredFish.Count, dm.MaxCapacity);
        }

        private void OnDestroy() => _disposables.Dispose();
    }
}