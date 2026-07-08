using UnityEditor;
using UnityEngine;
using FishingSystem.Fishing_Pattern;

[CustomEditor(typeof(PatternDataSO))]
public class PatternDataSOEditor : Editor
{
    private SerializedProperty loopPattern;
    private SerializedProperty patternNodes;
    
    // 미리보기(그래프)를 위한 가상의 낚시터 범위
    private float previewRange = 5f;

    private void OnEnable()
    {
        loopPattern = serializedObject.FindProperty("loopPattern");
        patternNodes = serializedObject.FindProperty("patternNodes");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🐟 물고기 이동 패턴 에디터", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("이동 시간(초)이 클수록 물고기가 천천히 이동하여 플레이어가 따라잡기 쉬워집니다.", MessageType.Info);
        
        EditorGUILayout.PropertyField(loopPattern);
        
        EditorGUILayout.Space(10);
        previewRange = EditorGUILayout.FloatField("가상 낚시터 범위 (미리보기용)", previewRange);
        EditorGUILayout.Space(5);

        for (int i = 0; i < patternNodes.arraySize; i++)
        {
            SerializedProperty node = patternNodes.GetArrayElementAtIndex(i);
            SerializedProperty targetX = node.FindPropertyRelative("targetPositionX");
            SerializedProperty moveDur = node.FindPropertyRelative("moveDuration");
            SerializedProperty wait = node.FindPropertyRelative("waitTime");

            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"▶ 노드 {i + 1}", EditorStyles.boldLabel, GUILayout.Width(70));
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                patternNodes.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            // 목표 좌표를 슬라이더로 조절할 수 있게 
            targetX.floatValue = EditorGUILayout.Slider("목표 X 위치", targetX.floatValue, -previewRange, previewRange);
            
            EditorGUILayout.BeginHorizontal();
            moveDur.floatValue = EditorGUILayout.FloatField("이동 시간 (초)", moveDur.floatValue);
            wait.floatValue = EditorGUILayout.FloatField("도착 대기 (초)", wait.floatValue);
            EditorGUILayout.EndHorizontal();

            // 🎨 시각적 그래프 (미니맵) 그리기
            DrawNodeGraph(targetX.floatValue);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ 새 패턴 노드 추가", GUILayout.Height(30)))
        {
            patternNodes.arraySize++;
            // 새 노드 생성 시 기본값 세팅
            SerializedProperty newNode = patternNodes.GetArrayElementAtIndex(patternNodes.arraySize - 1);
            newNode.FindPropertyRelative("targetPositionX").floatValue = 0f;
            newNode.FindPropertyRelative("moveDuration").floatValue = 1.5f;
            newNode.FindPropertyRelative("waitTime").floatValue = 0.5f;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawNodeGraph(float xPos)
    {
        Rect rect = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
        
        // 배경 가이드 라인 (낚시터 범위)
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + 9, rect.width, 2), Color.gray);
        
        // 정중앙(0) 표시선
        EditorGUI.DrawRect(new Rect(rect.x + rect.width / 2, rect.y + 3, 1, 14), Color.white);

        // 물고기 현재 목표 위치를 네모 박스로 표시
        float normalizedX = Mathf.InverseLerp(-previewRange, previewRange, xPos);
        float dotX = Mathf.Lerp(rect.x, rect.x + rect.width - 12, normalizedX);
        
        EditorGUI.DrawRect(new Rect(dotX, rect.y + 4, 12, 12), Color.cyan);
    }
}