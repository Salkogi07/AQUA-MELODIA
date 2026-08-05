using UnityEngine;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI.FishingMiniGame
{
    public class FishingMiniGameUIPresenter : MonoBehaviour
    {
        [SerializeField] private FishingMiniGameUIView view;
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

            // UI 보관함 활성화 구독
            this.model.IsMiniGameActive.Subscribe(isActive =>
            {
                view.SetActive(isActive);
            }).AddTo(this);

            // 플레이어 게이지 비율 구독
            this.model.PlayerReelRatio.Subscribe(ratio =>
            {
                view.UpdatePlayerReel(ratio);
            }).AddTo(this);

            // 물고기 게이지 비율 구독
            this.model.FishUiRatio.Subscribe(ratio =>
            {
                view.UpdateFishPosition(ratio);
            }).AddTo(this);

            // 스트레스 게이지 비율 구독
            this.model.LineStress.Subscribe(stress =>
            {
                view.UpdateStress(stress);
            }).AddTo(this);

            // 물고기 기력 비율 구독
            this.model.FishHpRatio.Subscribe(hp =>
            {
                view.UpdateFishHp(hp);
            }).AddTo(this);

            // 오차범위 계산 병합 구독
            Observable.CombineLatest(
                    this.model.PlayerReelRatio, 
                    this.model.FishUiRatio, 
                    (player, fish) => Mathf.Abs(player - fish) <= this.model.sweetSpotTolerance
                )
                .Subscribe(isSafe => 
                {
                    view.SetSweetSpotFeedback(isSafe);
                })
                .AddTo(this);
        }
    }
}