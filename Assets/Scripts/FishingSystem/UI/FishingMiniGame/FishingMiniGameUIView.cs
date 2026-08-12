using UnityEngine;
using UnityEngine.UI;

namespace FishingSystem.UI.FishingMiniGame
{
    public class FishingMiniGameUIView : MonoBehaviour
    {
        [SerializeField] private GameObject uiContainer;
        [SerializeField] private Image playerReelFill; 
        [SerializeField] private Image fishPositionFill; 
        [SerializeField] private Image stressFill; 
        [SerializeField] private Image fishHpFill; 

        [Header("색상 피드백 프리셋")]
        [SerializeField] private Color safeColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f);

        public void SetActive(bool isActive)
        {
            if (uiContainer != null) 
                uiContainer.SetActive(isActive);
        }

        public void UpdatePlayerReel(float ratio)
        {
            if (playerReelFill != null) playerReelFill.fillAmount = ratio;
        }

        public void UpdateFishPosition(float ratio)
        {
            if (fishPositionFill != null) fishPositionFill.fillAmount = ratio;
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
            if (playerReelFill != null)
            {
                playerReelFill.color = isSafe ? safeColor : dangerColor;
            }
        }
    }
}