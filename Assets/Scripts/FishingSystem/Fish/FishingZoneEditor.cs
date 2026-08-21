using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FishingSystem.Bait;

namespace FishingSystem.Fish
{
    [CustomEditor(typeof(FishingZone))]
    public class FishingZoneEditor : Editor
    {
        private FishingZone zone;
        private Dictionary<FishGrade, bool> foldoutStates = new Dictionary<FishGrade, bool>();

        private void OnEnable()
        {
            zone = (FishingZone)target;
        }

        private bool GetFoldoutState(FishGrade grade)
        {
            if (!foldoutStates.ContainsKey(grade)) foldoutStates[grade] = true;
            return foldoutStates[grade];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<b>🔍 디버그 설정</b>", new GUIStyle(EditorStyles.label) { richText = true });
            zone.enableDebugLog = EditorGUILayout.Toggle("확률 로그 출력 활성화", zone.enableDebugLog);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"<size=13><b>[ {zone.zoneName} ] 설정</b></size>", new GUIStyle(EditorStyles.label) { richText = true });

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            zone.zoneName = EditorGUILayout.TextField("낚시터 이름", zone.zoneName);
            zone.zoneRegion = (FishingRegion)EditorGUILayout.EnumPopup("지역 유형", zone.zoneRegion); 
            zone.maxMoveRange = EditorGUILayout.FloatField("최대 이동 범위", zone.maxMoveRange);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            DrawGradeProbabilities();

            EditorGUILayout.Space(10);
            DrawFishListGrouped();

            DrawUnassignedFish();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(zone);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawGradeProbabilities()
        {
            EditorGUILayout.LabelField("<b>1. 등급별 확률</b>", new GUIStyle(EditorStyles.label) { richText = true });
            float total = zone.gradeChances.Sum(g => g.probability);

            if (Mathf.Abs(total - 100f) > 0.1f)
                EditorGUILayout.HelpBox($"합계: {total:F2}% (100% 필수)", MessageType.Error);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            for (int i = 0; i < zone.gradeChances.Count; i++)
            {
                var gc = zone.gradeChances[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(gc.grade.ToString(), GUILayout.Width(60));
                float newProb = EditorGUILayout.Slider(gc.probability, 0f, 100f);
                if (newProb != gc.probability)
                {
                    Undo.RecordObject(zone, "Change Probability");
                    var updated = zone.gradeChances[i];
                    updated.probability = newProb;
                    zone.gradeChances[i] = updated;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Normalize (100%)"))
            {
                Undo.RecordObject(zone, "Normalize");
                NormalizeProbabilities();
            }
            EditorGUILayout.EndVertical();
        }

        private void NormalizeProbabilities()
        {
            float total = zone.gradeChances.Sum(g => g.probability);
            if (total <= 0) return;
            for (int i = 0; i < zone.gradeChances.Count; i++)
            {
                var gc = zone.gradeChances[i];
                gc.probability = (gc.probability / total) * 100f;
                zone.gradeChances[i] = gc;
            }
        }

        private void DrawFishListGrouped()
        {
            EditorGUILayout.LabelField("<b>2. 물고기 배치 및 가중치</b>", new GUIStyle(EditorStyles.label) { richText = true });

            foreach (FishGrade grade in System.Enum.GetValues(typeof(FishGrade)))
            {
                var gradeInfo = zone.gradeChances.FirstOrDefault(g => g.grade == grade);
                var fishInGrade = zone.fishSpawnList.Where(f => f.fishData != null && f.fishData.grade == grade).ToList();
                // GetValue() 호출로 전면 수정
                int totalWeight = fishInGrade.Sum(f => f.weight.GetValue()); 

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                bool isExpanded = GetFoldoutState(grade);
                foldoutStates[grade] = EditorGUILayout.Foldout(isExpanded, $"{grade} ({gradeInfo.probability:F1}%) - {fishInGrade.Count}종", true, EditorStyles.foldoutHeader);

                if (foldoutStates[grade])
                {
                    EditorGUILayout.Space(2);
                    foreach (var entry in fishInGrade)
                    {
                        DrawFishEntry(entry, totalWeight, gradeInfo.probability);
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUI.color = new Color(0.8f, 1f, 0.8f);
                    FishDataSO newFish = (FishDataSO)EditorGUILayout.ObjectField(null, typeof(FishDataSO), false);
                    GUI.color = Color.white;
                    if (newFish != null)
                    {
                        if (newFish.grade == grade)
                        {
                            Undo.RecordObject(zone, "Add Fish");
                            FishSpawnEntry newEntry = new FishSpawnEntry { fishData = newFish };
                            newEntry.weight.SetDefaultValue(10); 
                            zone.fishSpawnList.Add(newEntry);
                        }
                        else EditorUtility.DisplayDialog("경고", "등급이 맞지 않습니다.", "확인");
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawFishEntry(FishSpawnEntry entry, int totalWeight, float gradeProb)
        {
            Rect containerRect = EditorGUILayout.BeginVertical();
            GUILayout.Space(42); 
            EditorGUILayout.EndVertical();

            Rect boxRect = new Rect(containerRect.x + 5, containerRect.y + 2, containerRect.width - 10, 38);
            GUI.Box(boxRect, "", EditorStyles.helpBox);

            Rect iconRect = new Rect(boxRect.x + 5, boxRect.y + 5, 28, 28);
            if (entry.fishData.fishSprite != null)
            {
                Texture2D texture = AssetPreview.GetAssetPreview(entry.fishData.fishSprite);
                if (texture != null) GUI.DrawTexture(iconRect, texture);
            }
            else GUI.Box(iconRect, "");

            Rect nameRect = new Rect(boxRect.x + 38, boxRect.y + 4, boxRect.width - 70, 18);
            GUI.Label(nameRect, entry.fishData.fishName, EditorStyles.boldLabel);

            Rect xBtnRect = new Rect(boxRect.xMax - 25, boxRect.y + 4, 20, 18);
            if (GUI.Button(xBtnRect, "X"))
            {
                Undo.RecordObject(zone, "Remove Fish");
                zone.fishSpawnList.Remove(entry);
                return;
            }

            Rect weightLabelRect = new Rect(boxRect.x + 38, boxRect.y + 20, 40, 16);
            GUI.Label(weightLabelRect, "가중치", EditorStyles.miniLabel);

            Rect weightFieldRect = new Rect(boxRect.x + 75, boxRect.y + 20, 35, 16);
            
            // GetValue() 호출로 전면 수정
            int currentVal = entry.weight.GetValue();
            int newWeight = EditorGUI.IntField(weightFieldRect, currentVal);
            if (newWeight != currentVal)
            {
                Undo.RecordObject(zone, "Change Weight");
                entry.weight.SetDefaultValue(Mathf.Max(1, newWeight));
            }

            float inGradeChance = totalWeight > 0 ? ((float)entry.weight.GetValue() / totalWeight) * 100f : 0f;
            float overallChance = (gradeProb / 100f) * inGradeChance;
            string chanceText = $"등급 내:{inGradeChance:F1}% | 전체:{overallChance:F2}%";
            
            GUIStyle chanceStyle = new GUIStyle(EditorStyles.miniLabel);
            chanceStyle.alignment = TextAnchor.MiddleRight;
            chanceStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            Rect chanceRect = new Rect(boxRect.x + 115, boxRect.y + 20, boxRect.width - 125, 16);
            GUI.Label(chanceRect, chanceText, chanceStyle);
        }

        private void DrawUnassignedFish()
        {
            var unassigned = zone.fishSpawnList.Where(f => f.fishData == null).ToList();
            if (unassigned.Count > 0)
            {
                EditorGUILayout.Space(10);
                if (GUILayout.Button("데이터 오류 항목 청소", GUILayout.Height(25)))
                {
                    Undo.RecordObject(zone, "Clean");
                    zone.fishSpawnList.RemoveAll(f => f.fishData == null);
                }
            }
        }
    }
}