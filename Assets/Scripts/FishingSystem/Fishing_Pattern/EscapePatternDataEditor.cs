#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace FishingSystem.Fishing_Pattern
{
    [CustomEditor(typeof(EscapePatternData))]
    public class EscapePatternDataEditor : Editor
    {
        private EscapePatternData _targetData;
        private SerializedProperty _pointsProperty;

        private int _selectedPointIndex = -1;
        private bool _isDragging = false;

        private float _zoomScale = 40f;
        private Vector2 _panOffset = Vector2.zero;
        private bool _isPanning = false;

        private const float MAX_PAN_LIMIT = 500f;

        private void OnEnable()
        {
            _targetData = (EscapePatternData)target;
            _pointsProperty = serializedObject.FindProperty("points");
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            VisualElement line = new VisualElement();
            line.style.height = 2;
            line.style.backgroundColor = Color.gray;
            line.style.marginTop = 10;
            line.style.marginBottom = 10;
            root.Add(line);

            Label title = new Label("🎯 패턴 시각적 편집기 (Visual Editor)");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.marginBottom = 5;
            root.Add(title);

            Slider zoomSlider = new Slider("🔍 캔버스 줌 배율", 5f, 150f);
            zoomSlider.value = _zoomScale;
            zoomSlider.RegisterValueChangedCallback(evt =>
            {
                _zoomScale = evt.newValue;
                Repaint();
            });
            zoomSlider.style.marginBottom = 10;
            root.Add(zoomSlider);

            IMGUIContainer previewContainer = new IMGUIContainer(() => DrawPatternCanvas());
            previewContainer.style.height = 350;
            previewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            previewContainer.style.borderBottomWidth = 1;
            previewContainer.style.borderBottomColor = Color.black;
            previewContainer.style.marginBottom = 10;
            root.Add(previewContainer);

            VisualElement buttonGroup = new VisualElement();
            buttonGroup.style.flexDirection = FlexDirection.Row;
            buttonGroup.style.justifyContent = Justify.SpaceAround;

            Button addBtn = new Button(() =>
            {
                serializedObject.Update();
                _pointsProperty.InsertArrayElementAtIndex(_pointsProperty.arraySize);

                if (_pointsProperty.arraySize > 1)
                {
                    Vector2 lastPos = _pointsProperty.GetArrayElementAtIndex(_pointsProperty.arraySize - 2)
                        .vector2Value;
                    _pointsProperty.GetArrayElementAtIndex(_pointsProperty.arraySize - 1).vector2Value =
                        lastPos + new Vector2(1f, 0f);
                }
                else
                {
                    _pointsProperty.GetArrayElementAtIndex(0).vector2Value = Vector2.zero;
                }

                serializedObject.ApplyModifiedProperties();
                Repaint();
            }) { text = "➕ 점 추가 (Add Point)" };
            addBtn.style.flexGrow = 1;
            addBtn.style.height = 30;

            Button removeBtn = new Button(() =>
            {
                if (_pointsProperty.arraySize > 0)
                {
                    serializedObject.Update();
                    _pointsProperty.DeleteArrayElementAtIndex(_pointsProperty.arraySize - 1);
                    serializedObject.ApplyModifiedProperties();
                    _selectedPointIndex = -1;
                    Repaint();
                }
            }) { text = "❌ 마지막 점 삭제" };
            removeBtn.style.flexGrow = 1;
            removeBtn.style.height = 30;
            removeBtn.style.marginLeft = 5;

            buttonGroup.Add(addBtn);
            buttonGroup.Add(removeBtn);
            root.Add(buttonGroup);

            // ----------------- 하단 고정 가이드 박스 UI -----------------
            VisualElement guideBox = new VisualElement();
            guideBox.style.marginTop = 12;

            guideBox.style.paddingTop = 10;
            guideBox.style.paddingBottom = 10;
            guideBox.style.paddingLeft = 10;
            guideBox.style.paddingRight = 10;

            guideBox.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);

            guideBox.style.borderTopWidth = 1;
            guideBox.style.borderBottomWidth = 1;
            guideBox.style.borderLeftWidth = 1;
            guideBox.style.borderRightWidth = 1;

            guideBox.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            guideBox.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            guideBox.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            guideBox.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f, 1f);

            guideBox.style.borderTopLeftRadius = 6;
            guideBox.style.borderTopRightRadius = 6;
            guideBox.style.borderBottomLeftRadius = 6;
            guideBox.style.borderBottomRightRadius = 6;

            Label guideTitle = new Label("💡 캔버스 조작 가이드");
            guideTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            guideTitle.style.fontSize = 12;
            guideTitle.style.color = new Color(0.9f, 0.7f, 0.1f, 1f);
            guideTitle.style.marginBottom = 6;
            guideBox.Add(guideTitle);

            Label helpLabel = new Label(
                "• [노란색 점 드래그] : 마우스 좌클릭으로 경로 위치 편집\n" +
                "• [마우스 휠 스크롤] : 캔버스 공간 확대 / 축소 (Zoom)\n" +
                "• [마우스 휠 버튼 클릭 드래그] : 카메라 시점 이동 (Pan)\n" +
                "• [범위 차단] : 캔버스 밖 영역의 정점은 자동으로 숨겨집니다.\n" +
                "• [원점 복귀] : 원점이 화면을 벗어나면 가장자리에 빨간 버튼이 생성됩니다.");
            helpLabel.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            helpLabel.style.fontSize = 11;

            guideBox.Add(helpLabel);
            root.Add(guideBox);

            return root;
        }

        private void DrawPatternCanvas()
        {
            Rect rect = GUILayoutUtility.GetRect(10, 1000, 350, 350);
            Event e = Event.current;

            Vector2 center = rect.center + _panOffset;

            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                _zoomScale = Mathf.Clamp(_zoomScale - e.delta.y * 2f, 5f, 150f);
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && e.button == 2)
            {
                _isPanning = true;
                e.Use();
            }

            if (_isPanning && e.type == EventType.MouseDrag)
            {
                _panOffset += e.delta;
                _panOffset.x = Mathf.Clamp(_panOffset.x, -MAX_PAN_LIMIT, MAX_PAN_LIMIT);
                _panOffset.y = Mathf.Clamp(_panOffset.y, -MAX_PAN_LIMIT, MAX_PAN_LIMIT);
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && e.button == 2)
            {
                _isPanning = false;
            }

            GUI.BeginGroup(rect);
            Vector2 localCenter = center - rect.position;

            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Handles.DrawLine(new Vector3(0, localCenter.y, 0), new Vector3(rect.width, localCenter.y, 0));
            Handles.DrawLine(new Vector3(localCenter.x, 0, 0), new Vector3(localCenter.x, rect.height, 0));

            if (localCenter.x >= 0 && localCenter.x <= rect.width && localCenter.y >= 0 && localCenter.y <= rect.height)
            {
                Handles.Label(new Vector3(localCenter.x + 5, localCenter.y + 5, 0), "(0, 0) 원점",
                    EditorStyles.miniLabel);
            }

            IReadOnlyList<Vector2> points = _targetData.Points;
            List<Vector2> screenPositions = new List<Vector2>();

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 scrPos = localCenter + new Vector2(points[i].x * _zoomScale, -points[i].y * _zoomScale);
                screenPositions.Add(scrPos);
            }

            if (screenPositions.Count > 1)
            {
                Handles.color = Color.cyan;
                for (int i = 0; i < screenPositions.Count - 1; i++)
                {
                    Vector3 startPos = new Vector3(screenPositions[i].x, screenPositions[i].y, 0);
                    Vector3 endPos = new Vector3(screenPositions[i + 1].x, screenPositions[i + 1].y, 0);

                    Handles.DrawAAPolyLine(3f, startPos, endPos);

                    Vector3 dir = (endPos - startPos).normalized;
                    Vector3 mid = (startPos + endPos) * 0.5f;

                    Handles.DrawAAPolyLine(2f, mid, mid - Quaternion.Euler(0, 0, 30) * dir * 8f);
                    Handles.DrawAAPolyLine(2f, mid, mid - Quaternion.Euler(0, 0, -30) * dir * 8f);
                }
            }

            float handleRadius = 7f;
            Vector2 localMousePos = e.mousePosition - rect.position;

            for (int i = 0; i < screenPositions.Count; i++)
            {
                if (screenPositions[i].x < 0 || screenPositions[i].x > rect.width ||
                    screenPositions[i].y < 0 || screenPositions[i].y > rect.height)
                {
                    continue;
                }

                Vector3 targetPos = new Vector3(screenPositions[i].x, screenPositions[i].y, 0);

                Handles.color = (_selectedPointIndex == i) ? Color.green : Color.yellow;
                Handles.DrawSolidDisc(targetPos, Vector3.forward, handleRadius);
                Handles.Label(targetPos + new Vector3(8, -8, 0), $"[{i}]", EditorStyles.boldLabel);

                if (e.type == EventType.MouseDown && e.button == 0 &&
                    Vector2.Distance(localMousePos, screenPositions[i]) <= handleRadius + 3f)
                {
                    _selectedPointIndex = i;
                    _isDragging = true;
                    e.Use();
                }
            }

            if (_isDragging && e.type == EventType.MouseDrag && _selectedPointIndex != -1 && e.button == 0)
            {
                Vector2 mouseOffset = localMousePos - localCenter;
                Vector2 newWorldPos = new Vector2(mouseOffset.x / _zoomScale, -mouseOffset.y / _zoomScale);

                newWorldPos.x = Mathf.Round(newWorldPos.x * 100f) / 100f;
                newWorldPos.y = Mathf.Round(newWorldPos.y * 100f) / 100f;

                serializedObject.Update();
                _pointsProperty.GetArrayElementAtIndex(_selectedPointIndex).vector2Value = newWorldPos;
                serializedObject.ApplyModifiedProperties();

                Repaint();
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDragging = false;
            }

            GUI.EndGroup();

            bool isOriginOutsideX = (center.x < rect.x || center.x > rect.xMax);
            bool isOriginOutsideY = (center.y < rect.y || center.y > rect.yMax);

            if (isOriginOutsideX || isOriginOutsideY)
            {
                float indicatorX = Mathf.Clamp(center.x, rect.x + 15f, rect.xMax - 15f);
                float indicatorY = Mathf.Clamp(center.y, rect.y + 15f, rect.yMax - 15f);
                Vector2 indicatorPos = new Vector2(indicatorX, indicatorY);

                Handles.color = Color.red;
                Handles.DrawSolidDisc(new Vector3(indicatorPos.x, indicatorPos.y, 0), Vector3.forward, 6f);
                GUI.color = Color.red;

                string directionArrow = "";
                if (center.y < rect.y) directionArrow = "▲ ";
                else if (center.y > rect.yMax) directionArrow = "▼ ";
                if (center.x < rect.x) directionArrow += "◀ ";
                else if (center.x > rect.xMax) directionArrow += "▶ ";

                Rect clickTriggerRect = new Rect(indicatorPos.x - 45f, indicatorPos.y - 10f, 90f, 20f);
                if (GUI.Button(clickTriggerRect, directionArrow + "원점 복귀", EditorStyles.miniButton))
                {
                    _panOffset = Vector2.zero;
                    _isDragging = false;
                    _selectedPointIndex = -1;
                    e.Use();
                    Repaint();
                }

                GUI.color = Color.white;
            }
        }
    }
}
#endif