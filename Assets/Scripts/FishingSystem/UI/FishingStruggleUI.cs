using UnityEngine;
using UnityEngine.UI;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI
{
    public class FishingStruggleUI : MonoBehaviour
    {
        [Header("연결 참조")]
        [SerializeField] private FishingRod fishingRod;

        [Header("활성화 제어 대상")]
        [SerializeField] private GameObject struggleUI;

        [Header("남은 잉크 UI (Image Type: Filled 권장)")]
        [SerializeField] private Image inkGaugeFillImage;

        private void Start()
        {
            // 씬 시작 시 비활성 보장
            if (struggleUI != null) struggleUI.SetActive(false);

            // 1. 발악 상태(IsStruggleActive)에 따라 게임오브젝트와 UI 오브젝트 자동 활성화 연동
            fishingRod.IsStruggleActive
                .Subscribe(isActive =>
                {
                    if (struggleUI != null) struggleUI.SetActive(isActive);
                })
                .AddTo(this);

            // 2. PatternDrawer에 이미 내장되어 있던 반응형 CurrentInkRatio 프로퍼티를 드롭다운 타겟 UI에 바인딩
            if (fishingRod.patternDrawer != null && inkGaugeFillImage != null)
            {
                fishingRod.patternDrawer.CurrentInkRatio
                    .Subscribe(ratio =>
                    {
                        inkGaugeFillImage.fillAmount = ratio;
                    })
                    .AddTo(this);
            }
        }
    }
}