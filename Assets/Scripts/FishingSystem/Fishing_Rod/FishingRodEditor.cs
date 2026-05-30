#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FishingSystem.Fishing_Rod
{
    [CustomEditor(typeof(FishingRod))]
    public class FishingRodEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FishingRod fishingRod = (FishingRod)target;

            GUILayout.Space(15);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f); // 초록색 버튼 설정
            
            if (GUILayout.Button("🎣 테스트 찌 던지기 (Cast Bobber)", GUILayout.Height(40)))
            {
                if (Application.isPlaying)
                {
                    // UniTaskVoid 방식을 잊음(Forget) 처리하여 에디터 버튼에서 안정적으로 호출
                    fishingRod.CastBobberAsync().Forget();
                }
                else
                {
                    Debug.LogWarning("게임 플레이(Play) 모드에서만 던지기 테스트가 가능합니다!");
                }
            }
        }
    }
}
#endif