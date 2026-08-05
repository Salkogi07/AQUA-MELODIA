using UnityEngine;
using UnityEngine.UI;

namespace FishingSystem.UI.FishingCast
{
    public class FishingCastUIView : MonoBehaviour
    {
        [SerializeField] private GameObject gaugeContainer;
        [SerializeField] private Image gaugeFillImage;

        public void SetActive(bool isActive)
        {
            if (gaugeContainer != null) 
                gaugeContainer.SetActive(isActive);
        }

        public void UpdatePower(float value)
        {
            if (gaugeFillImage != null)
            {
                gaugeFillImage.fillAmount = value;
            }
        }
    }
}