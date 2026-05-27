using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;

namespace FishingSystem.Pattern
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

        public async UniTask GeneratePatternAsync(EscapePatternData patternData)
        {
            CancelPreviousTask();
            _patternCts = new CancellationTokenSource();
            CancellationToken token = _patternCts.Token;

            ClearCurrentPattern();

            if (patternData == null || patternData.Points.Count < 2) return;

            float currentVisualSpacing = patternData.DotSpacing > 0 ? patternData.DotSpacing : defaultDotSpacing;
            float currentDetectSpacing = patternData.DetectionSpacing > 0 ? patternData.DetectionSpacing : defaultDetectionSpacing;
            int currentDotsPerFrame = patternData.DotsPerFrame > 0 ? patternData.DotsPerFrame : defaultDotsPerFrame;
            int currentDrawDelayMs = patternData.DrawDelayMs >= 0 ? patternData.DrawDelayMs : defaultDrawDelayMs;

            var points = patternData.Points;
            Vector2 originPosition = (Vector2)transform.position;

            // 1. [판정선 맵 빌드] 표시 여부(showDetector) 인자 추가전달
            BuildDetectorLineImmediate(points, originPosition, currentDetectSpacing);

            // 2. [비주얼 점선 연출]
            int currentFrameDotCount = 0;
            try
            {
                Vector2 startOrigin = originPosition + points[0];
                PatternDot firstDot = dotPoolManager.GetDot();
                firstDot.transform.position = startOrigin;
                firstDot.SetColor(startDotColor);
                
                _activeDots.Add(firstDot);
                _onDotSpawned.OnNext(firstDot);
                currentFrameDotCount++;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector2 start = originPosition + points[i];
                    Vector2 end = originPosition + points[i + 1];
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
            firstDetector.transform.position = origin + points[0];
            firstDetector.InitializeDetector(detectorScale, detectorColor, showDetector);
            _activeDetectors.Add(firstDetector);

            float distanceLeftFromPreviousSegment = spacing;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = origin + points[i];
                Vector2 end = origin + points[i + 1];
                
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

            Vector2 finalPos = origin + points[^1];
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
    }
}