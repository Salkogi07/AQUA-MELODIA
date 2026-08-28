using UnityEngine;

namespace Scene
{
    public class LoadSelectMapScene : MonoBehaviour
    {
        public void LoadScene(string sceneName)
        {
            LoadingManager.instance.LoadScene(sceneName);
        }
    }
}