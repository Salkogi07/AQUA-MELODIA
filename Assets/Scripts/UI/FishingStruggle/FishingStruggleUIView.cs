using UnityEngine;
using UnityEngine.UI;

namespace FishingSystem.UI.FishingStruggle
{
    public class FishingStruggleUIView : MonoBehaviour
    {
        [SerializeField] private GameObject struggleObject;
        [SerializeField] private Image inkGaugeFillImage;

        public void SetActive(bool isActive)
        {
            if (struggleObject != null) struggleObject.SetActive(isActive);
        }

        public void UpdateInkRatio(float ratio)
        {
            if (inkGaugeFillImage != null)
            {
                inkGaugeFillImage.fillAmount = ratio;
            }
        }
    }
}