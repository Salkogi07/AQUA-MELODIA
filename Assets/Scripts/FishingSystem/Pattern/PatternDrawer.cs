using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Cysharp.Threading.Tasks;

namespace FishingSystem.Pattern
{
    public class PatternDrawer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private Camera mainCamera;
        [Tooltip("브러쉬 선이 생성될 부모 Transform 오브젝트")]
        [SerializeField] private Transform brushParent;

        [Header("Drawing & Line Custom Settings")]
        [Range(0.05f, 1.0f)]
        [SerializeField] private float lineWidth = 0.2f;
        
        [Tooltip("마우스 이동 감지 최소 거리")]
        [SerializeField] private float minDistanceBetweenPoints = 0.05f;

        [Tooltip("곡선 부드러움 정도 (기본값 5 추천)")]
        [Range(1, 10)]
        [SerializeField] private int curveSmoothingSteps = 5;

        // --- 추가된 잉크 시스템 변수 ---
        private float _maxInkDistance = -1f; // -1이면 무제한 (안전장치)
        private float _currentInkUsed = 0f;

        // UI에서 잉크 잔량을 표시하기 위한 ReactiveProperty (1.0 = 100%, 0.0 = 0%)
        public ReactiveProperty<float> CurrentInkRatio { get; } = new(1f);
        // -------------------------------

        private GameObject _currentLineObject;
        private LineRenderer _currentLineRenderer;
        private EdgeCollider2D _currentEdgeCollider;
        
        private readonly List<Vector2> _rawMousePoints = new List<Vector2>();
        private readonly List<Vector2> _linePoints = new List<Vector2>();
        private readonly List<GameObject> _drawnLines = new List<GameObject>();
        
        private readonly Subject<Unit> _onDrawStarted = new();
        public Observable<Unit> OnDrawStarted => _onDrawStarted;

        private readonly Subject<Unit> _onLineCompleted = new();
        public Observable<Unit> OnLineCompleted => _onLineCompleted;

        private bool _isDrawingNow = false;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Start()
        {
            HandleDrawingFlowAsync(destroyCancellationToken).Forget();
        }

        private void OnDisable()
        {
            ForceStopDrawing();
        }

        /// <summary>
        /// 컨트롤러에서 이 패턴을 그리기 위한 최대 잉크(거리)를 설정합니다.
        /// </summary>
        public void SetMaxInk(float maxInk)
        {
            _maxInkDistance = maxInk;
            _currentInkUsed = 0f;
            CurrentInkRatio.Value = 1f;
        }

        private async UniTaskVoid HandleDrawingFlowAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.WaitUntil(() => this.enabled && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame, cancellationToken: token);

                ClearAllDrawnLines();
                _onDrawStarted.OnNext(Unit.Default);
                StartDrawing();

                while (Mouse.current != null && Mouse.current.leftButton.isPressed && this.enabled)
                {
                    ContinueDrawing();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                }

                if (_isDrawingNow)
                {
                    StopDrawing();
                }
            }
        }

        private void StartDrawing()
        {
            _isDrawingNow = true;
            _rawMousePoints.Clear();
            _linePoints.Clear();
            
            // 그리기 시작할 때 잉크 초기화 (재시도 시)
            _currentInkUsed = 0f;
            CurrentInkRatio.Value = 1f;

            _currentLineObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, brushParent);
            _currentLineRenderer = _currentLineObject.GetComponent<LineRenderer>();
            _currentEdgeCollider = _currentLineObject.GetComponent<EdgeCollider2D>();

            ApplyLineSizeSettings();

            Vector2 mouseWorldPos = GetMouseWorldPosition();
            AddRawPoint(mouseWorldPos);
        }

        private void ContinueDrawing()
        {
            Vector2 mouseWorldPos = GetMouseWorldPosition();
            
            if (_rawMousePoints.Count > 0)
            {
                Vector2 lastPoint = _rawMousePoints[^1];
                float distance = Vector2.Distance(mouseWorldPos, lastPoint);
                
                if (distance > minDistanceBetweenPoints)
                {
                    // --- 잉크 제한 로직 ---
                    if (_maxInkDistance > 0)
                    {
                        if (_currentInkUsed + distance > _maxInkDistance)
                        {
                            float remainingInk = _maxInkDistance - _currentInkUsed;
                            if (remainingInk > 0)
                            {
                                // 남은 잉크만큼만 선을 연장하고 마우스 위치를 보정합니다.
                                Vector2 direction = (mouseWorldPos - lastPoint).normalized;
                                mouseWorldPos = lastPoint + direction * remainingInk;
                                distance = remainingInk;
                            }
                            else
                            {
                                // 잉크가 다 떨어지면 더 이상 점을 추가하지 않습니다.
                                CurrentInkRatio.Value = 0f;
                                return; 
                            }
                        }
                        
                        _currentInkUsed += distance;
                        CurrentInkRatio.Value = 1f - (_currentInkUsed / _maxInkDistance);
                    }
                    // ----------------------

                    AddRawPoint(mouseWorldPos);
                }
            }
        }

        private void StopDrawing()
        {
            if (_currentLineObject != null)
            {
                _drawnLines.Add(_currentLineObject);
            }

            _isDrawingNow = false;
            _onLineCompleted.OnNext(Unit.Default);
        }

        public void ForceStopDrawing()
        {
            if (_isDrawingNow)
            {
                StopDrawing();
            }
        }

        private void ApplyLineSizeSettings()
        {
            if (_currentLineRenderer != null)
            {
                _currentLineRenderer.startWidth = lineWidth;
                _currentLineRenderer.endWidth = lineWidth;
                _currentLineRenderer.numCornerVertices = 8;
                _currentLineRenderer.numCapVertices = 8;
            }

            if (_currentEdgeCollider != null)
            {
                _currentEdgeCollider.edgeRadius = lineWidth * 0.5f;
            }
        }

        private void AddRawPoint(Vector2 point)
        {
            _rawMousePoints.Add(point);
            int rawCount = _rawMousePoints.Count;

            if (rawCount == 1)
            {
                _linePoints.Add(point);
            }
            else if (rawCount == 2)
            {
                _linePoints.Add(point);
            }
            else
            {
                _linePoints.RemoveAt(_linePoints.Count - 1);

                Vector2 p0 = _rawMousePoints[rawCount - 3];
                Vector2 p1 = _rawMousePoints[rawCount - 2];
                Vector2 p2 = _rawMousePoints[rawCount - 1];

                Vector2 p0_p1_mid = (p0 + p1) / 2f;
                Vector2 p1_p2_mid = (p1 + p2) / 2f;

                if (rawCount == 3)
                {
                    _linePoints.Clear();
                    _linePoints.Add(p0);
                    _linePoints.Add(p0_p1_mid);
                }

                for (int i = 1; i <= curveSmoothingSteps; i++)
                {
                    float t = i / (float)curveSmoothingSteps;
                    Vector2 curvePoint = CalculateQuadraticBezierPoint(t, p0_p1_mid, p1, p1_p2_mid);
                    _linePoints.Add(curvePoint);
                }

                _linePoints.Add(p2);
            }

            UpdateLineRendererAndCollider();
        }

        private Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * p0) + (2 * u * t * p1) + (tt * p2);
        }

        private void UpdateLineRendererAndCollider()
        {
            int count = _linePoints.Count;
            _currentLineRenderer.positionCount = count;
    
            for (int i = 0; i < count; i++)
            {
                _currentLineRenderer.SetPosition(i, new Vector3(_linePoints[i].x, _linePoints[i].y, 0));
            }

            if (count > 1)
            {
                _currentEdgeCollider.SetPoints(_linePoints);
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = -mainCamera.transform.position.z; 
            return mainCamera.ScreenToWorldPoint(mousePos);
        }

        public void ClearAllDrawnLines()
        {
            foreach (var line in _drawnLines)
            {
                if (line != null) Destroy(line);
            }
            _drawnLines.Clear();
            if (_currentLineObject != null) Destroy(_currentLineObject);
        }

        private void OnDestroy()
        {
            _onDrawStarted.Dispose();
            _onLineCompleted.Dispose();
            CurrentInkRatio.Dispose();
        }
    }
}