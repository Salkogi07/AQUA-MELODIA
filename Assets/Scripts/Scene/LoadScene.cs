using FishingSystem.Island;
using UnityEngine;

namespace Scene
{
    public class LoadScene : MonoBehaviour
    {
        public void LoadSceneByName(string sceneName)
        {
            LoadingManager.instance.LoadScene(sceneName);
        }

        public void SelectIslandButton(IslandDataSO island)
        {
            IslandUIController.Instance.SetTargetIsland(island);
        }
    }
}