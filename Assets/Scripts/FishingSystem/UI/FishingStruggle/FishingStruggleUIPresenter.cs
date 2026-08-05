using UnityEngine;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI.FishingStruggle
{
    public class FishingStruggleUIPresenter : MonoBehaviour
    {
        [SerializeField] private FishingStruggleUIView view;
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

            // 1. 발악 상태에 따른 월드 및 캔버스 비주얼 오너십 활성화
            this.model.IsStruggleActive.Subscribe(isActive =>
            {
                view.SetActive(isActive);
            }).AddTo(this);

            // 2. 드로어(PatternDrawer)에 내장된 잉크 소모 비유율 실시간 바인딩
            if (this.model.patternDrawer != null)
            {
                this.model.patternDrawer.CurrentInkRatio.Subscribe(ratio =>
                {
                    view.UpdateInkRatio(ratio);
                }).AddTo(this);
            }
        }
    }
}