using UnityEngine;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI.FishingCast
{
    public class FishingCastUIPresenter : MonoBehaviour
    {
        [SerializeField] private FishingCastUIView view;
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

            // 1. 게이지 컨테이너 온/오프 상태 바인딩
            this.model.IsCharging.Subscribe(isCharging =>
            {
                view.SetActive(isCharging);
            }).AddTo(this);

            // 2. 파워 비율 이미지 바인딩
            this.model.CastPower.Subscribe(power =>
            {
                view.UpdatePower(power);
            }).AddTo(this);
        }
    }
}