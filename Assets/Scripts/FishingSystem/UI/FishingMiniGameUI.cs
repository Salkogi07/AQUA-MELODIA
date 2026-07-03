using UnityEngine;
using UnityEngine.UI;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.UI
{
    public class FishingMiniGameUI : MonoBehaviour
    {
        [Header("연결 참조")]
        public FishingRod fishingRod;
        public GameObject uiContainer;

        [Header("미니게임 트래킹 UI (Image Type: Filled 권장)")]
        [Tooltip("플레이어가 릴을 감은 정도 (회색 영역)")]
        public Image playerReelFill; 
        
        [Tooltip("물고기의 현재 위치 (게이지로 표현할 경우)")]
        public Image fishPositionFill; 

        [Header("기타 정보 UI (Image Type: Filled 권장)")]
        [Tooltip("스트레스(위험도) 게이지 바")]
        public Image stressFill; 
        [Tooltip("물고기 체력 게이지 바")]
        public Image fishHpFill; 

        [Header("색상 변경 연출 (선택)")]
        public Color safeColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 오차범위 안 (기본 회색)
        public Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f); // 오차범위 밖 (붉은색 톤)

        private void Start()
        {
            uiContainer.SetActive(false); 

            // 1. 미니게임 활성화 시 UI 켜기
            fishingRod.IsMiniGameActive
                .Subscribe(isActive => uiContainer.SetActive(isActive))
                .AddTo(this);

            // 2. 플레이어의 조준(감은) 위치 업데이트 -> Image의 fillAmount 사용!
            if (playerReelFill != null)
            {
                fishingRod.PlayerReelRatio
                    .Subscribe(ratio => playerReelFill.fillAmount = ratio)
                    .AddTo(this);
            }

            // 3. 물고기 위치 업데이트 -> Image의 fillAmount 사용!
            if (fishPositionFill != null)
            {
                fishingRod.FishUiRatio
                    .Subscribe(ratio => fishPositionFill.fillAmount = ratio)
                    .AddTo(this);
            }

            // 4. 스트레스 및 물고기 체력 업데이트
            if (stressFill != null)
            {
                fishingRod.LineStress
                    .Subscribe(stress => stressFill.fillAmount = stress)
                    .AddTo(this);
            }
            
            if (fishHpFill != null)
            {
                fishingRod.FishHpRatio
                    .Subscribe(hp => fishHpFill.fillAmount = hp)
                    .AddTo(this);
            }

            // 5. 오차범위(10%)에 따른 시각적 피드백 (플레이어 릴 색상 변경)
            if (playerReelFill != null)
            {
                // R3의 CombineLatest를 사용해 두 값이 변할 때마다 오차를 계산합니다.
                Observable.CombineLatest(
                    fishingRod.PlayerReelRatio, 
                    fishingRod.FishUiRatio, 
                    (player, fish) => Mathf.Abs(player - fish) <= fishingRod.sweetSpotTolerance
                )
                .Subscribe(isSafe => 
                {
                    playerReelFill.color = isSafe ? safeColor : dangerColor;
                })
                .AddTo(this);
            }
        }
    }
}