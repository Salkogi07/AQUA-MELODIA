using UnityEngine;
using UnityEngine.UI;

namespace FishingSystem.UI.FishingMiniGame
{
    public class FishingMiniGameUIView : MonoBehaviour
    {
        [SerializeField] private GameObject uiContainer;
        [Tooltip("초록 조준 범위(Handle)를 가진 슬라이더")]
        [SerializeField] private Slider stickSlider;
        [Tooltip("물고기 아이콘(Handle)을 가진 슬라이더")]
        [SerializeField] private Slider fishSlider;
        [SerializeField] private Image stressFill;
        [SerializeField] private Image fishHpFill;

        [Header("색상 피드백 프리셋")]
        [SerializeField] private Color safeColor = Color.white;
        [SerializeField] private Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f);

        private void Awake()
        {
            // 표시 전용 슬라이더: 마우스 드래그 및 색상 틴트 차단
            DisableSliderInput(stickSlider);
            DisableSliderInput(fishSlider);
        }

        private static void DisableSliderInput(Slider slider)
        {
            if (slider == null) return;
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
        }

        public void SetActive(bool isActive)
        {
            if (uiContainer != null)
                uiContainer.SetActive(isActive);
        }

        public void UpdatePlayerReel(float ratio)
        {
            if (stickSlider != null) stickSlider.SetValueWithoutNotify(ratio);
        }

        public void UpdateFishPosition(float ratio)
        {
            if (fishSlider != null) fishSlider.SetValueWithoutNotify(ratio);
        }

        // 초록 조준 범위 너비를 슬라이드 영역 대비 비율(0~1)로 맞춥니다.
        public void UpdateSweetSpotSize(float sizeRatio)
        {
            if (stickSlider == null || stickSlider.handleRect == null) return;

            RectTransform handle = stickSlider.handleRect;
            RectTransform slideArea = (RectTransform)handle.parent;
            float width = Mathf.Round(sizeRatio * slideArea.rect.width); // 픽셀 정렬
            handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        public void UpdateStress(float stress)
        {
            if (stressFill != null) stressFill.fillAmount = stress;
        }

        public void UpdateFishHp(float hpRatio)
        {
            if (fishHpFill != null) fishHpFill.fillAmount = hpRatio;
        }

        public void SetSweetSpotFeedback(bool isSafe)
        {
            if (stickSlider != null && stickSlider.targetGraphic != null)
            {
                stickSlider.targetGraphic.color = isSafe ? safeColor : dangerColor;
            }
        }
    }
}
