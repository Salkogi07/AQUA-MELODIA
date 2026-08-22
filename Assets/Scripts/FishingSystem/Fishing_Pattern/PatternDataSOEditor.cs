using UnityEditor;
using UnityEngine;
using FishingSystem.Fishing_Pattern;
using System.Collections.Generic;

[CustomEditor(typeof(PatternDataSO))]
public class PatternDataSOEditor : Editor
{
    private SerializedProperty loopPattern;
    private SerializedProperty patternNodes;
    
    // 미리보기(그래프)를 위한 가상의 낚시터 범위
    private float previewRange = 5f;

    // --- 시뮬레이션 상태 변수들 ---
    private bool isSimulating = false;
    private double lastTimeSinceStartup = 0f;
    private float simCurrentPositionX = 0f;
    private float simStartPositionX = 0f;
    private int simCurrentNodeIndex = 0;
    private float simElapsedInNode = 0f;
    private bool simIsWaiting = false;

    // 가상 스탯 설정 (민첩성 상쇄 테스트용)
    private float mockFishAgility = 0f;
    private float mockRodAgility = 0f;

    private void OnEnable()
    {
        loopPattern = serializedObject.FindProperty("loopPattern");
        patternNodes = serializedObject.FindProperty("patternNodes");
        
        // 에디터 업데이트 루프 등록 (실시간 시뮬레이션 구동용)
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
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
        DrawPatternNodesList();

        EditorGUILayout.Space(15);
        DrawSimulationPanel();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPatternNodesList()
    {
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
            moveDur.floatValue = Mathf.Max(0.05f, EditorGUILayout.FloatField("이동 시간 (초)", moveDur.floatValue));
            wait.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("도착 대기 (초)", wait.floatValue));
            EditorGUILayout.EndHorizontal();

            // 시각적 그래프 (미니맵) 그리기
            DrawNodeGraph(targetX.floatValue);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ 새 패턴 노드 추가", GUILayout.Height(30)))
        {
            patternNodes.arraySize++;
            SerializedProperty newNode = patternNodes.GetArrayElementAtIndex(patternNodes.arraySize - 1);
            newNode.FindPropertyRelative("targetPositionX").floatValue = 0f;
            newNode.FindPropertyRelative("moveDuration").floatValue = 1.5f;
            newNode.FindPropertyRelative("waitTime").floatValue = 0.5f;
        }
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

    /// <summary>
    /// 실시간 시뮬레이터 패널 UI를 구성합니다.
    /// </summary>
    private void DrawSimulationPanel()
    {
        EditorGUILayout.LabelField("🎮 실시간 패턴 미리보기 및 스탯 시뮬레이터", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 1. 시뮬레이션용 임의 스탯 컨트롤
        EditorGUILayout.LabelField("스탯 설정", EditorStyles.miniBoldLabel);
        
        mockFishAgility = EditorGUILayout.Slider("물고기 민첩 (Agility)", mockFishAgility, 0f, 30f);
        mockRodAgility = EditorGUILayout.Slider("낚싯대 민첩 (Rod Agility)", mockRodAgility, 0f, 30f);
        
        // [수정] 낚싯대 민첩상쇄 차감 공식 실시간 계산
        float remainingAgility = Mathf.Max(0f, mockFishAgility - mockRodAgility);
        float speedMultiplier = 1f + (remainingAgility * 0.2f);
        speedMultiplier = Mathf.Clamp(speedMultiplier, 1.0f, 5.0f); // 최소 1배(기본 제 속도) ~ 최대 5배속

        EditorGUILayout.Space(2);
        if (remainingAgility <= 0f)
        {
            // 낚싯대가 물고기 민첩을 완벽히 제압했을 때
            EditorGUILayout.LabelField("효과 분석: 🟢 물고기 제압 상태 (기본 속도 1.0배로 안정적이게 이동)");
        }
        else
        {
            // 물고기가 낚싯대보다 민첩하여 가속 상태일 때
            EditorGUILayout.LabelField($"효과 분석: 🟡 물고기 가속 상태 (기본 속도 대비 {speedMultiplier:F1}배 빠르게 이동 / 미상쇄 민첩: {remainingAgility:F1})");
        }
        EditorGUILayout.Space(5);

        // 2. 재생 컨트롤 버튼들
        EditorGUILayout.BeginHorizontal();
        if (isSimulating)
        {
            if (GUILayout.Button("정지 (Stop)", GUILayout.Height(25)))
            {
                isSimulating = false;
            }
        }
        else
        {
            if (GUILayout.Button("재생 미리보기 (Play)", GUILayout.Height(25)))
            {
                if (patternNodes.arraySize > 0)
                {
                    ResetSimulation();
                    isSimulating = true;
                    lastTimeSinceStartup = EditorApplication.timeSinceStartup;
                }
                else
                {
                    EditorUtility.DisplayDialog("경보", "시뮬레이션할 패턴 노드가 존재하지 않습니다.", "확인");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 3. 실시간 애니메이션 캔버스 영역
        Rect simRect = GUILayoutUtility.GetRect(10, 45, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(simRect, new Color(0.15f, 0.15f, 0.15f, 1f));

        // 캔버스 가이드라인
        EditorGUI.DrawRect(new Rect(simRect.x, simRect.y + simRect.height / 2f, simRect.width, 2), new Color(0.35f, 0.35f, 0.35f)); // 전체 축선
        EditorGUI.DrawRect(new Rect(simRect.x + simRect.width / 2f, simRect.y + 10, 1, simRect.height - 20), new Color(0.5f, 0.5f, 0.5f, 0.5f)); // 정중앙(0) 점선 대용

        // 3a. 모든 노드 정점의 위치 표시
        for (int i = 0; i < patternNodes.arraySize; i++)
        {
            float targetX = patternNodes.GetArrayElementAtIndex(i).FindPropertyRelative("targetPositionX").floatValue;
            float normX = Mathf.InverseLerp(-previewRange, previewRange, targetX);
            float screenX = Mathf.Lerp(simRect.x, simRect.x + simRect.width - 10, normX);
            
            // 일반 노드는 회색, 현재 목표 대상 노드는 하늘색 테두리 처리
            bool isActiveTarget = isSimulating && (simCurrentNodeIndex == i);
            Color nodeColor = isActiveTarget ? Color.cyan : new Color(0.6f, 0.6f, 0.6f);
            
            EditorGUI.DrawRect(new Rect(screenX, simRect.y + 12, 8, 8), nodeColor);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = nodeColor } };
            GUI.Label(new Rect(screenX - 4, simRect.y + 1, 20, 12), (i + 1).ToString(), labelStyle);
        }

        // 3b. 실시간 물고기 위치 연출
        float fishNormX = Mathf.InverseLerp(-previewRange, previewRange, simCurrentPositionX);
        float fishScreenX = Mathf.Lerp(simRect.x, simRect.x + simRect.width - 14, fishNormX);
        
        // 물고기 아이콘 또는 심볼 사각형 드로잉 (노란색 원형/사각형 사양)
        EditorGUI.DrawRect(new Rect(fishScreenX, simRect.y + simRect.height / 2f - 6, 12, 12), new Color(1f, 0.75f, 0f));

        // 4. 상태 출력 텍스트 영역
        EditorGUILayout.Space(5);
        if (isSimulating && patternNodes.arraySize > 0)
        {
            var currentNode = patternNodes.GetArrayElementAtIndex(simCurrentNodeIndex);
            float currentTargetX = currentNode.FindPropertyRelative("targetPositionX").floatValue;
            float baseDuration = currentNode.FindPropertyRelative("moveDuration").floatValue;
            float effectiveDuration = baseDuration / speedMultiplier;
            float waitTime = currentNode.FindPropertyRelative("waitTime").floatValue;

            string statusText = simIsWaiting 
                ? $"대기 중... (대기 시간: {simElapsedInNode:F1}s / {waitTime:F1}s)" 
                : $"이동 중 ▶ 노드 {simCurrentNodeIndex + 1} (진행 시간: {simElapsedInNode:F1}s / {effectiveDuration:F1}s)";

            EditorGUILayout.LabelField($"현재 상태: {statusText}");
            EditorGUILayout.LabelField($"물고기 가상 위치(X): {simCurrentPositionX:F2} (목표: {currentTargetX:F2})");
        }
        else
        {
            EditorGUILayout.LabelField("시뮬레이션 상태: 대기 중 (Play 버튼을 누르면 구동됩니다)");
        }

        EditorGUILayout.EndVertical();
    }

    private void ResetSimulation()
    {
        simCurrentPositionX = 0f;
        simStartPositionX = 0f;
        simCurrentNodeIndex = 0;
        simElapsedInNode = 0f;
        simIsWaiting = false;
    }

    /// <summary>
    /// 에디터 업데이트 주기마다 시뮬레이션 데이터를 실시간 프레임 시간 기반으로 갱신합니다.
    /// </summary>
    private void OnEditorUpdate()
    {
        if (!isSimulating || patternNodes == null || patternNodes.arraySize == 0) return;

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - lastTimeSinceStartup);
        lastTimeSinceStartup = currentTime;

        // 에디터가 정지 중 등의 오차 보정 예외처리
        if (deltaTime > 0.1f) deltaTime = 0.1f; 

        // 민첩성 상쇄 비율 연산 매핑 동기화
        float remainingAgility = Mathf.Max(0f, mockFishAgility - mockRodAgility);
        float speedMultiplier = 1f + (remainingAgility * 0.2f);
        speedMultiplier = Mathf.Clamp(speedMultiplier, 1.0f, 5.0f);

        SerializedProperty currentNode = patternNodes.GetArrayElementAtIndex(simCurrentNodeIndex);
        float targetX = currentNode.FindPropertyRelative("targetPositionX").floatValue;
        float baseDuration = currentNode.FindPropertyRelative("moveDuration").floatValue;
        float effectiveDuration = Mathf.Max(0.01f, baseDuration / speedMultiplier); // 실제 보정이 가해진 이동 타임
        float waitTime = currentNode.FindPropertyRelative("waitTime").floatValue;

        simElapsedInNode += deltaTime;

        if (!simIsWaiting)
        {
            // 이동 페이즈
            float progress = Mathf.Clamp01(simElapsedInNode / effectiveDuration);
            simCurrentPositionX = Mathf.Lerp(simStartPositionX, targetX, progress);

            if (progress >= 1.0f)
            {
                if (waitTime > 0f)
                {
                    simIsWaiting = true;
                    simElapsedInNode = 0f;
                }
                else
                {
                    ProceedToNextNode();
                }
            }
        }
        else
        {
            // 대기 페이즈
            if (simElapsedInNode >= waitTime)
            {
                ProceedToNextNode();
            }
        }

        // 인스펙터 실시간 강제 드로우 갱신
        Repaint();
    }

    private void ProceedToNextNode()
    {
        simStartPositionX = simCurrentPositionX;
        simElapsedInNode = 0f;
        simIsWaiting = false;

        int nodeCount = patternNodes.arraySize;
        if (simCurrentNodeIndex + 1 < nodeCount)
        {
            simCurrentNodeIndex++;
        }
        else
        {
            if (loopPattern.boolValue)
            {
                simCurrentNodeIndex = 0;
            }
            else
            {
                isSimulating = false; // 루프가 꺼져있다면 마지막 노드에서 종료
            }
        }
    }
}