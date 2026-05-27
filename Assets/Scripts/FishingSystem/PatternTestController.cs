using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using FishingSystem.Pattern;
using R3;

namespace FishingSystem.Pattern
{
    public class PatternTestController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PatternGenerator patternGenerator;
        [SerializeField] private PatternEvaluator patternEvaluator;
        [SerializeField] private PatternDrawer patternDrawer; 

        [Header("Test Settings")]
        [SerializeField] private EscapePatternData testPattern;
        [Tooltip("패턴이 모두 생성된 후 주어지는 드로잉 시간 (초)")]
        [SerializeField] private float drawTimeLimit = 3f;
        
        [Tooltip("전체 패턴 길이 대비 제공할 잉크량의 배수 (1.2면 20% 여유 제공)")]
        [SerializeField] private float inkBufferMultiplier = 1.2f;

        private bool _isTesting = false;

        private void Start()
        {
            SetDrawingEnabled(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (!_isTesting)
                {
                    StartTestRoutine().Forget();
                }
            }
        }

        private async UniTaskVoid StartTestRoutine()
        {
            _isTesting = true;

            // 1. 상태 초기화
            InitializeTest();
            Debug.Log("[테스트] 패턴을 생성합니다...");

            // 2. 잉크량 계산 및 설정
            float totalPatternLength = CalculatePatternLength(testPattern);
            float maxInk = totalPatternLength * inkBufferMultiplier;
            patternDrawer.SetMaxInk(maxInk);

            // 3. 패턴 비주얼 생성 대기
            await patternGenerator.GeneratePatternAsync(testPattern);

            // 4. 생성 완료 -> 그리기 및 판정 시작
            Debug.Log($"<color=green>[테스트] 생성 완료! {drawTimeLimit}초 동안 선을 그리세요! (제공된 잉크: {maxInk:F2})</color>");
            SetDrawingEnabled(true);
            patternEvaluator.StartEvaluation();

            // 5. 제한 시간 대기
            await UniTask.Delay(TimeSpan.FromSeconds(drawTimeLimit));

            // 6. 시간 초과 -> 즉시 그리기 강제 종료 및 판정 시스템 정지
            if (patternDrawer != null)
            {
                patternDrawer.ForceStopDrawing();
            }
            SetDrawingEnabled(false);
            patternEvaluator.StopEvaluation();

            // 7. 결과 산출 (R3 규칙 적용)
            float finalScore = patternEvaluator.CompletionProgress.CurrentValue;
            Debug.Log($"<color=cyan>[테스트 종료]</color> 최종 포획 완성도: {finalScore:F1}%");

            _isTesting = false;
        }

        public void InitializeTest()
        {
            SetDrawingEnabled(false);
            patternDrawer.ClearAllDrawnLines();
            patternGenerator.ClearCurrentPattern();
            patternEvaluator.ResetEvaluation();
        }

        private void SetDrawingEnabled(bool isEnabled)
        {
            if (patternDrawer != null)
            {
                patternDrawer.enabled = isEnabled; 
            }
        }

        /// <summary>
        /// 패턴 데이터의 점들 사이의 총 길이를 계산합니다.
        /// </summary>
        private float CalculatePatternLength(EscapePatternData data)
        {
            if (data == null || data.Points == null || data.Points.Count < 2) 
                return 0f;

            float length = 0f;
            for (int i = 0; i < data.Points.Count - 1; i++)
            {
                length += Vector2.Distance(data.Points[i], data.Points[i + 1]);
            }
            return length;
        }
    }
}