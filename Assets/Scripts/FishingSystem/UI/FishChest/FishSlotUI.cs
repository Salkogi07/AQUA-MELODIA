using System;
using UnityEngine;
using UnityEngine.UI;
using FishingSystem.Fish;

namespace FishingSystem.UI
{
    public class FishSlotUI : MonoBehaviour
    {
        [Header("슬롯 내부 UI 구성요소")]
        [SerializeField] private Image fishSpriteImage;     // 물고기 이미지 컴포넌트
        [SerializeField] private Text fishNameText;          // 물고기 이름 텍스트
        [SerializeField] private Text fishDetailsText;       // 등급, 품질, 크기 종합 정보 텍스트

        private void Awake()
        {
            fishSpriteImage.type = Image.Type.Simple;
            fishSpriteImage.preserveAspect = true;
        }

        /// <summary>
        /// 인계받은 개별 물고기 데이터를 분석하여 슬롯 UI 구성 요소에 정밀 사상합니다.
        /// </summary>
        public void SetupSlot(FishData fish)
        {
            if (fish == null) return;

            // 1. 물고기 스프라이트 렌더링
            if (fishSpriteImage != null)
            {
                if (fish.Data.fishSprite != null)
                {
                    fishSpriteImage.sprite = fish.Data.fishSprite;
                    fishSpriteImage.gameObject.SetActive(true);
                }
                else
                {
                    // 등록된 전용 이미지가 없는 경우 감춤 처리
                    fishSpriteImage.gameObject.SetActive(false); 
                }
            }

            // 2. 이름 텍스트 갱신
            if (fishNameText != null)
            {
                fishNameText.text = fish.Data.fishName;
            }

            // 3. 디테일 속성 텍스트 갱신
            if (fishDetailsText != null)
            {
                fishDetailsText.text = $"품질: {fish.Quality}\n크기: {fish.Length:F1}cm";
            }
        }
    }
}