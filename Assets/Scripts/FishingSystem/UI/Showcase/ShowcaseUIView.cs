using UnityEngine;
using UnityEngine.UI;

namespace FishingSystem.UI.Showcase
{
    public class ShowcaseUIView : MonoBehaviour
    {
        [SerializeField] private GameObject uiContainer;
        [SerializeField] private Text fishNameText;
        [SerializeField] private Text fishLengthText;

        public void SetActive(bool isActive)
        {
            if (uiContainer != null) 
                uiContainer.SetActive(isActive);
        }

        public void UpdateFishInfo(string fishName, float length)
        {
            if (fishNameText != null) 
                fishNameText.text = fishName;
                
            if (fishLengthText != null) 
                fishLengthText.text = $"{length:F1} cm";
        }
    }
}