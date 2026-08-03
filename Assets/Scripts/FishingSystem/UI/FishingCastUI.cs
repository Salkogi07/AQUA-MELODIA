using UnityEngine;
using UnityEngine.UI;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI
{
    public class FishingCastUI : MonoBehaviour
    {
        [Header("연결 참조")]
        public FishingRod fishingRod;
        public GameObject gaugeContainer;

        [Header("게이지 바 (Image Type: Filled 권장)")]
        public Image gaugeFillImage;

        private void Start()
        {
            if (gaugeContainer != null)
                gaugeContainer.SetActive(false);

            // 1. 충전 여부에 따른 UI 표시/숨김 연동
            fishingRod.IsCharging
                .Subscribe(isCharging => {
                    if (gaugeContainer != null)
                        gaugeContainer.SetActive(isCharging);
                })
                .AddTo(this);

            // 2. 충전 비율 실시간 반영
            if (gaugeFillImage != null)
            {
                fishingRod.CastPower
                    .Subscribe(power => gaugeFillImage.fillAmount = power)
                    .AddTo(this);
            }
        }
    }
}