using System;
using System.Collections.Generic;
using UnityEngine;
using R3;

namespace FishingSystem.Fishing_Pattern
{
    public class PatternEvaluator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PatternGenerator patternGenerator;

        // 최종 포획 완성도 (0% ~ 100%)
        public ReactiveProperty<float> CompletionProgress { get; } = new(0f);

        private CompositeDisposable _evaluationDisposables = new();
        private int _totalDetectors = 0;
        private int _triggeredCount = 0;

        /// <summary>
        /// 평가 상태를 초기화합니다.
        /// </summary>
        public void ResetEvaluation()
        {
            _evaluationDisposables.Dispose();
            _evaluationDisposables = new CompositeDisposable();
            
            CompletionProgress.Value = 0f;
            _totalDetectors = 0;
            _triggeredCount = 0;
        }

        /// <summary>
        /// 판정을 시작합니다. 생성된 판정선(Detector)들의 트리거 이벤트를 구독합니다.
        /// </summary>
        public void StartEvaluation()
        {
            ResetEvaluation();

            var activeDetectors = patternGenerator.ActiveDetectors;
            _totalDetectors = activeDetectors.Count;

            if (_totalDetectors == 0)
            {
                CompletionProgress.Value = 0f;
                return;
            }

            foreach (var detector in activeDetectors)
            {
                // 이미 트리거된 상태라면 (혹시 모를 예외 처리)
                if (detector.IsTriggered.Value)
                {
                    _triggeredCount++;
                }

                // R3: 판정선이 트리거되는 순간을 구독하여 점수를 올림
                detector.IsTriggered
                    .Where(isTriggered => isTriggered)
                    .Subscribe(_ => 
                    {
                        _triggeredCount++;
                        UpdateProgress();
                    })
                    .AddTo(_evaluationDisposables);
            }

            // 초기 진행도 세팅
            UpdateProgress();
        }

        /// <summary>
        /// 평가를 강제로 멈춥니다. 시간 초과 시 호출하여 더 이상 점수가 오르지 않도록 합니다.
        /// </summary>
        public void StopEvaluation()
        {
            _evaluationDisposables.Dispose();
            _evaluationDisposables = new CompositeDisposable();
        }

        private void UpdateProgress()
        {
            if (_totalDetectors == 0) return;
            
            // 0 ~ 100 사이의 퍼센트로 계산
            float progress = ((float)_triggeredCount / _totalDetectors) * 100f;
            CompletionProgress.Value = Mathf.Clamp(progress, 0f, 100f);
        }

        private void OnDestroy()
        {
            _evaluationDisposables.Dispose();
            CompletionProgress.Dispose();
        }
    }
}