using UnityEngine;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI.Showcase
{
    public class ShowcaseUIPresenter : MonoBehaviour
    {
        [SerializeField] private ShowcaseUIView view;
        [SerializeField] private FishingRod model;

        private void Start()
        {
            if (model != null)
            {
                SetPlayerModel(model);
            }
        }

        public void SetPlayerModel(FishingRod model)
        {
            this.model = model;

            // 모델의 ShowcaseFish 상태를 실시간 관측하여 수치 대입 및 가시성 제어
            this.model.ShowcaseFish
                .Subscribe(fish =>
                {
                    if (fish != null)
                    {
                        view.UpdateFishInfo(fish.Data.fishName, fish.Length);
                        view.SetActive(true);
                    }
                    else
                    {
                        view.SetActive(false);
                    }
                })
                .AddTo(this);
        }
    }
}