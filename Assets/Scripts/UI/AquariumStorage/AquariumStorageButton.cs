using UnityEngine;

namespace UI.AquariumStorage
{
    public class AquariumStorageButton : MonoBehaviour
    {
        [SerializeField] private GameObject iceBoxPanel;
        
        public void SetActivePanel()
        {
            iceBoxPanel.SetActive(!iceBoxPanel.activeSelf);
        }
    }
}