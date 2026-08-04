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
        /// 판정을 시작합니다. 생성된 판정선(Detector)들의 트리거 이벤트를 구독하며, 새 그리기가 감지되면 데이터를 리셋합니다.
        /// </summary>
        public void StartEvaluation(PatternDrawer drawer)
        {
            ResetEvaluation();

            var activeDetectors = patternGenerator.ActiveDetectors;
            _totalDetectors = activeDetectors.Count;

            if (_totalDetectors == 0)
            {
                CompletionProgress.Value = 0f;
                return;
            }

            // 1. 각 디텍터들이 선과 충돌했을 때 스코어링 처리 구독
            foreach (var detector in activeDetectors)
            {
                if (detector.IsTriggered.Value)
                {
                    _triggeredCount++;
                }

                detector.IsTriggered
                    .Where(isTriggered => isTriggered)
                    .Subscribe(_ => 
                    {
                        _triggeredCount++;
                        UpdateProgress();
                    })
                    .AddTo(_evaluationDisposables);
            }

            // 2. [추가] 제한시간 내 마우스를 떼고 새로 그리기 시작했을 때의 동적 리셋 통합 관리
            if (drawer != null)
            {
                drawer.OnDrawStarted
                    .Subscribe(_ =>
                    {
                        // 모든 디텍터 판정을 다시 활성 상태(False)로 롤백
                        foreach (var detector in activeDetectors)
                        {
                            detector.ResetDetector();
                        }

                        // 내부 채점 변수 정화 및 UI 진행도 초기화
                        _triggeredCount = 0;
                        UpdateProgress();
                    })
                    .AddTo(_evaluationDisposables);
            }

            // 최초 수치 반영
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