using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;

namespace FishingSystem.Fishing_Pattern
{
    public class PatternGenerator : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private DotPoolManager dotPoolManager;
        [SerializeField] private DetectorPoolManager detectorPoolManager; 

        [Header("기본 백업 설정")]
        [SerializeField] private float defaultDotSpacing = 0.3f;
        [SerializeField] private float defaultDetectionSpacing = 0.1f;
        [SerializeField] private int defaultDotsPerFrame = 3; 
        [SerializeField] private int defaultDrawDelayMs = 10;

        [Header("도트 색상 설정")]
        [SerializeField] private Color startDotColor = Color.green;
        [SerializeField] private Color normalDotColor = Color.white;
        [SerializeField] private Color endDotColor = Color.red;

        [Header("판정선(Detector) 설정")]
        [Tooltip("체크하면 게임 뷰에서 판정선이 시각적으로 보입니다. (디버그용)")]
        [SerializeField] private bool showDetector = true; 
        [Tooltip("판정선 오브젝트의 Scale 크기를 조절합니다.")]
        [SerializeField] private Vector3 detectorScale = Vector3.one;
        [SerializeField] private Color detectorColor = new Color(1f, 1f, 0f, 0.5f);

        [Header("에디터 기즈모 설정 (Scene View Preview)")]
        [SerializeField] private bool _drawGizmos = true;
        [Tooltip("씬 뷰에서 미리보기할 패턴 에셋을 지정합니다.")]
        [SerializeField] private EscapePatternDataSO _previewPatternData;
        [Tooltip("패턴 데이터 편집기(EscapePatternDataSOEditor)의 CANVAS_LIMIT과 일치하게 설정하십시오.")]
        [SerializeField] private Vector2 _canvasLimit = new Vector2(4f, 4f);
        [SerializeField] private Color _boundaryColor = new Color(1f, 0f, 0f, 0.4f);
        [SerializeField] private Color _pathLineColor = Color.cyan;

        [Header("동적 크기 조절 설정")]
        [Tooltip("실제 인게임에서 생성될 패턴의 크기 비율입니다 (0.5 = 50% 축소 생성)")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float _generationScale = 1.0f;
        public float GenerationScale { get => _generationScale; set => _generationScale = Mathf.Clamp(value, 0.1f, 2.0f); }

        private readonly Subject<PatternDot> _onDotSpawned = new();
        public Observable<PatternDot> OnDotSpawned => _onDotSpawned;

        private readonly Subject<Unit> _onPatternComplete = new();
        public Observable<Unit> OnPatternComplete => _onPatternComplete;

        private readonly List<PatternDot> _activeDots = new();
        public IReadOnlyList<PatternDot> ActiveDots => _activeDots;

        private readonly List<PatternDetector> _activeDetectors = new();
        public IReadOnlyList<PatternDetector> ActiveDetectors => _activeDetectors;

        private CancellationTokenSource _patternCts;

        private void OnDestroy()
        {
            _onDotSpawned.OnCompleted();
            _onPatternComplete.OnCompleted();
            CancelPreviousTask();
        }

        public async UniTask GeneratePatternAsync(EscapePatternDataSO patternDataSo)
        {
            CancelPreviousTask();
            _patternCts = new CancellationTokenSource();
            CancellationToken token = _patternCts.Token;

            ClearCurrentPattern();

            if (patternDataSo == null || patternDataSo.Points.Count < 2) return;

            float currentVisualSpacing = patternDataSo.DotSpacing > 0 ? patternDataSo.DotSpacing : defaultDotSpacing;
            float currentDetectSpacing = patternDataSo.DetectionSpacing > 0 ? patternDataSo.DetectionSpacing : defaultDetectionSpacing;
            int currentDotsPerFrame = patternDataSo.DotsPerFrame > 0 ? patternDataSo.DotsPerFrame : defaultDotsPerFrame;
            int currentDrawDelayMs = patternDataSo.DrawDelayMs >= 0 ? patternDataSo.DrawDelayMs : defaultDrawDelayMs;

            var points = patternDataSo.Points;
            Vector2 originPosition = (Vector2)transform.position;

            // 1. [판정선 맵 빌드] 표시 여부(showDetector) 인자 추가전달
            BuildDetectorLineImmediate(points, originPosition, currentDetectSpacing);

            // 2. [비주얼 점선 연출]
            int currentFrameDotCount = 0;
            try
            {
                Vector2 startOrigin = originPosition + (points[0] * _generationScale);
                PatternDot firstDot = dotPoolManager.GetDot();
                firstDot.transform.position = startOrigin;
                firstDot.SetColor(startDotColor);
                
                _activeDots.Add(firstDot);
                _onDotSpawned.OnNext(firstDot);
                currentFrameDotCount++;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector2 start = originPosition + (points[i] * _generationScale);
                    Vector2 end = originPosition + (points[i + 1] * _generationScale);
                    float segmentLength = Vector2.Distance(start, end);

                    int visualCount = Mathf.FloorToInt(segmentLength / currentVisualSpacing);

                    for (int j = 1; j <= visualCount; j++)
                    {
                        token.ThrowIfCancellationRequested();
                        float progress = (float)j / visualCount;

                        Vector2 spawnPos = Vector2.Lerp(start, end, progress);
                        PatternDot dot = dotPoolManager.GetDot();
                        dot.transform.position = spawnPos;
                        dot.SetColor(normalDotColor);
                        
                        _activeDots.Add(dot);
                        _onDotSpawned.OnNext(dot);
                        
                        currentFrameDotCount++;

                        if (currentFrameDotCount >= currentDotsPerFrame)
                        {
                            currentFrameDotCount = 0;
                            if (currentDrawDelayMs > 0)
                            {
                                await UniTask.Delay(TimeSpan.FromMilliseconds(currentDrawDelayMs), cancellationToken: token);
                            }
                            else
                            {
                                await UniTask.Yield(PlayerLoopTiming.Update, token);
                            }
                        }
                    }
                }

                if (_activeDots.Count > 0)
                {
                    _activeDots[^1].SetColor(endDotColor);
                }

                _onPatternComplete.OnNext(Unit.Default);
            }
            catch (OperationCanceledException)
            {
                // 취소 처리
            }
        }

        private void BuildDetectorLineImmediate(IReadOnlyList<Vector2> points, Vector2 origin, float spacing)
        {
            if (points.Count < 2) return;

            PatternDetector firstDetector = detectorPoolManager.GetDetector();
            firstDetector.transform.position = origin + (points[0] * _generationScale);
            firstDetector.InitializeDetector(detectorScale, detectorColor, showDetector);
            _activeDetectors.Add(firstDetector);

            float distanceLeftFromPreviousSegment = spacing;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = origin + (points[i] * _generationScale);
                Vector2 end = origin + (points[i + 1] * _generationScale);
                
                Vector2 direction = end - start;
                float segmentLength = direction.magnitude;
                
                if (segmentLength < 0.001f) continue;
                
                direction.Normalize();

                float currentDistance = distanceLeftFromPreviousSegment;

                while (currentDistance <= segmentLength)
                {
                    Vector2 spawnPos = start + direction * currentDistance;

                    PatternDetector detector = detectorPoolManager.GetDetector();
                    detector.transform.position = spawnPos;
                    detector.InitializeDetector(detectorScale, detectorColor, showDetector);
                    _activeDetectors.Add(detector);

                    currentDistance += spacing;
                }

                distanceLeftFromPreviousSegment = currentDistance - segmentLength;
            }

            Vector2 finalPos = origin + (points[^1] * _generationScale);
            if (_activeDetectors.Count > 0 && Vector2.Distance(_activeDetectors[^1].transform.position, finalPos) > (spacing * 0.5f))
            {
                PatternDetector finalDetector = detectorPoolManager.GetDetector();
                finalDetector.transform.position = finalPos;
                finalDetector.InitializeDetector(detectorScale, detectorColor, showDetector);
                _activeDetectors.Add(finalDetector);
            }
        }

        public void ClearCurrentPattern()
        {
            foreach (var dot in _activeDots)
            {
                if (dot != null && dot.gameObject.activeSelf) dot.ReleaseToPool();
            }
            _activeDots.Clear();

            foreach (var detector in _activeDetectors)
            {
                if (detector != null && detector.gameObject.activeSelf) detector.ReleaseToPool();
            }
            _activeDetectors.Clear();
        }

        private void CancelPreviousTask()
        {
            if (_patternCts != null)
            {
                _patternCts.Cancel();
                _patternCts.Dispose();
                _patternCts = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;

            Vector3 origin = transform.position;

            // [수정] 배율(_generationScale)이 반영된 사각형 경계 영역 크기 계산
            Gizmos.color = _boundaryColor;
            Vector3 boundarySize = new Vector3(
                _canvasLimit.x * 2f * _generationScale, 
                _canvasLimit.y * 2f * _generationScale, 
                0.05f
            );
            
            Gizmos.DrawWireCube(origin, boundarySize);

            Gizmos.color = new Color(_boundaryColor.r, _boundaryColor.g, _boundaryColor.b, 0.08f);
            Gizmos.DrawCube(origin, boundarySize);

            // 2. 프리뷰 패턴 표시 (크기 배율인 _generationScale을 곱해 적용된 크기로 그림)
            if (_previewPatternData != null)
            {
                IReadOnlyList<Vector2> points = _previewPatternData.Points;
                if (points == null || points.Count == 0) return;

                Vector3 previousPosition = origin + (Vector3)(points[0] * _generationScale);

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(previousPosition, 0.12f);

                for (int i = 1; i < points.Count; i++)
                {
                    Vector3 currentPosition = origin + (Vector3)(points[i] * _generationScale);

                    Gizmos.color = _pathLineColor;
                    Gizmos.DrawLine(previousPosition, currentPosition);

                    if (i == points.Count - 1)
                    {
                        Gizmos.color = Color.red; 
                    }
                    else
                    {
                        Gizmos.color = Color.yellow; 
                    }
                    Gizmos.DrawSphere(currentPosition, 0.09f);

                    previousPosition = currentPosition;
                }
            }
        }
    }
}