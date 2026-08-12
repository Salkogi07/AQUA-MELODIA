using UnityEngine;
using R3;
using FishingSystem.Data;

namespace FishingSystem.UI
{
    public class FishChestUIPresenter : MonoBehaviour
    {
        [SerializeField] private FishChestUIView view;

        private void Start()
        {
            var manager = FishingDataManager.Instance;
            if (manager == null) return;
            
            manager.OnFishAdded
                .Subscribe(_ =>
                {
                    RefreshAll(manager);
                })
                .AddTo(this);
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
    }
}