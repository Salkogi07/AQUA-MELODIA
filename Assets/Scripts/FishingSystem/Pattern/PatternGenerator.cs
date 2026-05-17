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
            
            // [데이터 반영] 스크립터블 데이터를 정확히 가져옴
            float currentDetectSpacing = patternData.DetectionSpacing > 0 ? patternData.DetectionSpacing : defaultDetectionSpacing;
            
            int currentDotsPerFrame = patternData.DotsPerFrame > 0 ? patternData.DotsPerFrame : defaultDotsPerFrame;
            int currentDrawDelayMs = patternData.DrawDelayMs >= 0 ? patternData.DrawDelayMs : defaultDrawDelayMs;

            var points = patternData.Points;
            Vector2 originPosition = (Vector2)transform.position;

            // =================================================================
            // 1. [판정선 맵 빌드] 데이터 스페이싱(currentDetectSpacing)을 반영하여 즉시 생성
            // =================================================================
            BuildDetectorLineImmediate(points, originPosition, currentDetectSpacing);

            // =================================================================
            // 2. [비주얼 점선 연출] 지정된 딜레이 규칙에 따라 순차 생성 (기존 유지)
            // =================================================================
            int currentFrameDotCount = 0;
            try
            {
                Vector2 startOrigin = originPosition + points[0];
                PatternDot firstDot = dotPoolManager.GetDot();
                firstDot.transform.position = startOrigin;
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

                _onPatternComplete.OnNext(Unit.Default);
            }
            catch (OperationCanceledException)
            {
                // 취소 처리
            }
        }

        /// <summary>
        /// [핵심 수정] 스크립터블 오브젝트의 DetectionSpacing 단위로 
        /// 전체 경로를 오차 없이 촘촘하게 채우는 고정 간격 알고리즘
        /// </summary>
        private void BuildDetectorLineImmediate(IReadOnlyList<Vector2> points, Vector2 origin, float spacing)
        {
            if (points.Count < 2) return;

            // 시작 지점(정점 0번)에 무조건 첫 번째 판정선 생성
            PatternDetector firstDetector = detectorPoolManager.GetDetector();
            firstDetector.transform.position = origin + points[0];
            _activeDetectors.Add(firstDetector);

            // 누적된 남은 거리를 추적하여 꺾인 선 영역에서도 등간격(spacing)이 유지되도록 함
            float distanceLeftFromPreviousSegment = spacing;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = origin + points[i];
                Vector2 end = origin + points[i + 1];
                
                Vector2 direction = end - start;
                float segmentLength = direction.magnitude;
                
                if (segmentLength < 0.001f) continue; // 거의 제자리인 정점 예외처리
                
                direction.Normalize();

                // 이전 선분에서 남은 간격 버퍼를 정산하면서 현재 선분 위를 spacing 만큼 전진
                float currentDistance = distanceLeftFromPreviousSegment;

                while (currentDistance <= segmentLength)
                {
                    // 방향 벡터를 따라 정확한 스페이싱 위치 계산
                    Vector2 spawnPos = start + direction * currentDistance;

                    PatternDetector detector = detectorPoolManager.GetDetector();
                    detector.transform.position = spawnPos;
                    _activeDetectors.Add(detector);

                    // 스크립터블에 기획된 Spacing 데이터만큼 전진
                    currentDistance += spacing;
                }

                // 선분의 끝에 도달했을 때, 다음 선분의 시작점까지 남은 오프셋 거리를 이월 계산
                distanceLeftFromPreviousSegment = currentDistance - segmentLength;
            }

            // [선택 사항] 패턴의 가장 마지막 정점 위치에도 완벽한 마무리를 위해 판정점을 강제 배치하고 싶다면 활성화
            // (이미 누적 배치로 마지막 근처까지 채워졌으므로 오차 범위 판정을 원할 때 추가합니다)
            Vector2 finalPos = origin + points[^1];
            if (_activeDetectors.Count > 0 && Vector2.Distance(_activeDetectors[^1].transform.position, finalPos) > (spacing * 0.5f))
            {
                PatternDetector finalDetector = detectorPoolManager.GetDetector();
                finalDetector.transform.position = finalPos;
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