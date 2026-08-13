using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FishingSystem.Fish;

namespace FishingSystem.UI
{
    public class FishSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("슬롯 내부 UI 구성요소")]
        [SerializeField] private Image fishSpriteImage;

        [Header("테두리 하이라이트 설정")]
        [Tooltip("슬롯의 외곽 테두리 이미지 컴포넌트를 연결하세요.")]
        [SerializeField] private Image borderImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;

        private FishData _fishData;
        private FishTooltipUI _tooltipUI; 
        private RectTransform _rectTransform;

        public FishData CurrentFishData => _fishData;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            if (fishSpriteImage != null)
            {
                fishSpriteImage.type = Image.Type.Simple;
                fishSpriteImage.preserveAspect = true;
            }

            // 초기 테두리 색상 지정
            if (borderImage != null)
            {
                borderImage.color = normalColor;
            }
        }

        public void SetupSlot(FishData fish, FishTooltipUI tooltipUI)
        {
            _fishData = fish;
            _tooltipUI = tooltipUI; 

            if (fish == null) return;

            if (fishSpriteImage != null)
            {
                if (fish.Data.fishSprite != null)
                {
                    fishSpriteImage.sprite = fish.Data.fishSprite;
                    fishSpriteImage.gameObject.SetActive(true);
                }
                else
                {
                    fishSpriteImage.gameObject.SetActive(false); 
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_fishData == null || _tooltipUI == null) return;

            // 시각 피드백: 테두리 색상 변경
            if (borderImage != null)
            {
                borderImage.color = highlightColor;
            }

            // 대기 시간 없이 즉시 슬롯 위치 기준으로 툴팁 노출
            if (_rectTransform != null)
            {
                _tooltipUI.Show(_fishData, _rectTransform);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelHover();
        }

        private void OnDisable()
        {
            CancelHover();
        }

        private void OnDestroy()
        {
            CancelHover();
        }

        private void CancelHover()
        {
            // 시각 피드백 원상 복구
            if (borderImage != null)
            {
                borderImage.color = normalColor;
            }

            // 즉시 툴팁 비활성화
            if (_tooltipUI != null)
            {
                _tooltipUI.Hide();
            }
        }
    }
}