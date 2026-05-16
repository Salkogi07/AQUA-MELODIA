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
        [SerializeField] private DotPoolManager poolManager;

        [Header("기본 백업 설정")]
        [SerializeField] private float defaultDotSpacing = 0.3f;
        [SerializeField] private int defaultDotsPerFrame = 3; 
        [SerializeField] private int defaultDrawDelayMs = 10;

        private readonly Subject<PatternDot> _onDotSpawned = new();
        public Observable<PatternDot> OnDotSpawned => _onDotSpawned;

        private readonly Subject<Unit> _onPatternComplete = new();
        public Observable<Unit> OnPatternComplete => _onPatternComplete;

        private readonly List<PatternDot> _activeDots = new();
        public IReadOnlyList<PatternDot> ActiveDots => _activeDots;

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

            float currentSpacing = patternData.DotSpacing > 0 ? patternData.DotSpacing : defaultDotSpacing;
            int currentDotsPerFrame = patternData.DotsPerFrame > 0 ? patternData.DotsPerFrame : defaultDotsPerFrame;
            int currentDrawDelayMs = patternData.DrawDelayMs >= 0 ? patternData.DrawDelayMs : defaultDrawDelayMs;

            var points = patternData.Points;
            int currentFrameDotCount = 0;
            Vector2 originPosition = (Vector2)transform.position;

            try
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector2 start = originPosition + points[i];
                    Vector2 end = originPosition + points[i + 1];
                    
                    float distance = Vector2.Distance(start, end);
                    int dotCount = Mathf.Max(1, Mathf.FloorToInt(distance / currentSpacing));

                    for (int j = 0; j < dotCount; j++)
                    {
                        token.ThrowIfCancellationRequested();

                        float t = (float)j / dotCount;
                        Vector2 spawnPos = Vector2.Lerp(start, end, t);

                        PatternDot dot = poolManager.GetDot();
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

                token.ThrowIfCancellationRequested();

                PatternDot finalDot = poolManager.GetDot();
                finalDot.transform.position = originPosition + points[^1];
                _activeDots.Add(finalDot);
                _onDotSpawned.OnNext(finalDot);

                _onPatternComplete.OnNext(Unit.Default);
            }
            catch (OperationCanceledException)
            {
                // 취소 예외 처리
            }
        }

        public void ClearCurrentPattern()
        {
            foreach (var dot in _activeDots)
            {
                if (dot != null && dot.gameObject.activeSelf)
                {
                    dot.ReleaseToPool();
                }
            }
            _activeDots.Clear();
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