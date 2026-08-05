using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FishingSystem.Data
{
    [CustomEditor(typeof(FishingDataManager))]
    public class FishingDataManagerEditor : UnityEditor.Editor
    {
        private bool _showStoredFish = true;
        private bool _showEncyclopedia = true;
        private bool _showMaxRecords = true;

        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 UI 그리기 (SerializeField 및 기본 필드들)
            DrawDefaultInspector();

            FishingDataManager manager = (FishingDataManager)target;

            // 게임 플레이 중이 아닐 때는 데이터가 없으므로 안내문구 출력 후 리턴
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("런타임(Play Mode) 상태에서 인벤토리 및 도감 데이터가 실시간으로 표시됩니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("📊 런타임 데이터 모니터링", EditorStyles.boldLabel);
            
            // 1. 현재 보관 중인 물고기 인벤토리 표시
            DrawStoredFishSection(manager);

            // 2. 도감 사전 (포획 누적 횟수) 표시
            DrawEncyclopediaSection(manager);

            // 3. 어종별 신기록 길이 표시
            DrawMaxRecordsSection(manager);

            // 값이 실시간으로 갱신되도록 Repaint 호출
            Repaint();
        }

        private void DrawStoredFishSection(FishingDataManager manager)
        {
            var storedFish = manager.StoredFish;
            string headerText = $"📦 보관 중인 물고기 ({storedFish.Count} / {manager.MaxCapacity})";
            
            _showStoredFish = EditorGUILayout.Foldout(_showStoredFish, headerText, true);
            if (_showStoredFish)
            {
                EditorGUI.indentLevel++;
                if (storedFish.Count == 0)
                {
                    EditorGUILayout.LabelField("보관함이 비어 있습니다.");
                }
                else
                {
                    for (int i = 0; i < storedFish.Count; i++)
                    {
                        var fish = storedFish[i];
                        string fishName = fish != null && fish.Data != null ? fish.Data.fishName : "Unknown";
                        float length = fish != null ? fish.Length : 0f;
                        
                        EditorGUILayout.LabelField($"[{i}] {fishName} ({length:F1}cm)");
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawEncyclopediaSection(FishingDataManager manager)
        {
            var encyclopedia = manager.Encyclopedia;
            string headerText = $"📖 도감 사전 (등록된 어종: {encyclopedia.Count}종)";

            _showEncyclopedia = EditorGUILayout.Foldout(_showEncyclopedia, headerText, true);
            if (_showEncyclopedia)
            {
                EditorGUI.indentLevel++;
                if (encyclopedia.Count == 0)
                {
                    EditorGUILayout.LabelField("아직 등록된 도감이 없습니다.");
                }
                else
                {
                    foreach (var kvp in encyclopedia)
                    {
                        string fishName = kvp.Key != null && kvp.Key.Data != null ? kvp.Key.Data.fishName : "Unknown";
                        int count = kvp.Value;
                        
                        EditorGUILayout.LabelField($"• {fishName}: 누적 {count}회 포획");
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawMaxRecordsSection(FishingDataManager manager)
        {
            // 내부 필드인 _maxRecords는 매니저 클래스에 퍼블릭 프로퍼티가 없으므로 Reflection으로 가져오거나, 
            // 매니저에 공개 프로퍼티를 추가하는 것이 좋습니다. 여기서는 Reflection을 사용해 안전하게 가져옵니다.
            var maxRecordsField = typeof(FishingDataManager).GetField("_maxRecords", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var maxRecords = maxRecordsField?.GetValue(manager) as Dictionary<FishingSystem.Fish.FishData, float>;

            string headerText = "🏆 어종별 신기록 (Max Length)";
            _showMaxRecords = EditorGUILayout.Foldout(_showMaxRecords, headerText, true);
            if (_showMaxRecords)
            {
                EditorGUI.indentLevel++;
                if (maxRecords == null || maxRecords.Count == 0)
                {
                    EditorGUILayout.LabelField("기록된 최대 크기가 없습니다.");
                }
                else
                {
                    foreach (var kvp in maxRecords)
                    {
                        string fishName = kvp.Key != null && kvp.Key.Data != null ? kvp.Key.Data.fishName : "Unknown";
                        float maxLength = kvp.Value;

                        EditorGUILayout.LabelField($"• {fishName}: 최고 {maxLength:F1}cm");
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}