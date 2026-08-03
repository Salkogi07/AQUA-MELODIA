using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fishing_Rod;
using FishingSystem.Fishing_Pattern;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class FinalStruggleState : FishingState
    {
        private CancellationTokenSource cts;

        public FinalStruggleState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            cts = new CancellationTokenSource();
            
            // 낚싯줄은 팽팽하게 유지, 찌 물리는 정지
            fishingRod.SetLineState(FishingLineState.Taut);
            fishingRod.ResetBobberPhysics();

            // 발악 패턴 활성화 상태 플래그 설정 (동기화된 UI 활성화 유도)
            fishingRod.IsStruggleActive.Value = true;

            Debug.Log("<color=red>🔥 [발악 패턴 시작] 마우스를 드래그하여 선을 따라 그리세요!</color>");
            fishingRod.patternCameraPoint.PlayCameraAction(.8f);
            StruggleRoutineAsync(cts.Token).Forget();
        }

        private async UniTaskVoid StruggleRoutineAsync(CancellationToken token)
        {
            var patternSystem = fishingRod.patternGenerator;
            var evaluator = fishingRod.patternEvaluator;
            var drawer = fishingRod.patternDrawer;

            // 물고기 데이터에서 발악 패턴을 가져옵니다.
            EscapePatternDataSO escapeData = fishingRod.CurrentHookedFish.Data.escapePatternData;
            
            // 안전장치: 매니저가 없거나 물고기에 발악 패턴이 없으면 그냥 B등급으로 자동 성공 처리
            if (patternSystem == null || evaluator == null || drawer == null || escapeData == null)
            {
                Debug.LogWarning("⚠️ 발악 패턴 데이터나 매니저가 없습니다. 일반 등급(B)으로 자동 성공 처리합니다.");
                fishingRod.CurrentHookedFish.Quality = FishQuality.GradeB;
                stateMachine.ChangeState(fishingRod.RetrievingState);
                return;
            }

            try
            {
                // 1. 초기화
                fishingRod.ResetPattern();

                // 2. 잉크량 설정
                float length = CalculatePatternLength(escapeData);
                float maxInk = length * escapeData.InkBufferMultiplier;
                drawer.SetMaxInk(maxInk);

                // 3. 점선 연출 생성 (비동기 대기)
                await patternSystem.GeneratePatternAsync(escapeData);
                
                // 4. 생성 완료 후 그리기 시작
                drawer.enabled = true;
                evaluator.StartEvaluation();

                // 5. 제한 시간 대기 (플레이어가 그리는 시간)
                await UniTask.Delay(System.TimeSpan.FromSeconds(escapeData.TimeLimit), cancellationToken: token);

                // 6. 평가 종료 및 판정
                drawer.ForceStopDrawing();
                drawer.enabled = false;
                evaluator.StopEvaluation();
                
                CameraManager.Instance.ResetCamera(.8f);
                
                float finalScore = evaluator.CompletionProgress.CurrentValue;
                
                // 7. 점수에 따른 품질 결정 및 결과 처리
                ApplyResult(finalScore);
            }
            catch (System.OperationCanceledException)
            {
                // 상태가 강제로 바뀌어 취소된 경우
                if (drawer != null) drawer.enabled = false;
            }
        }

        private void ApplyResult(float score)
        {
            Debug.Log($"<color=cyan>[발악 패턴 종료]</color> 최종 완성도: {score:F1}%");

            if (score >= 90f)
            {
                Debug.Log("<color=#FFD700>🌟 [Perfect] 최고 품질(S등급)로 제압했습니다!</color>");
                fishingRod.CurrentHookedFish.Quality = FishQuality.GradeS;
                stateMachine.ChangeState(fishingRod.CaughtState);
            }
            else if (score >= 60f)
            {
                Debug.Log("<color=#00FF00>✨ [Good] 우수 품질(A등급)로 제압했습니다!</color>");
                fishingRod.CurrentHookedFish.Quality = FishQuality.GradeA;
                stateMachine.ChangeState(fishingRod.CaughtState);
            }
            else if (score >= 30f)
            {
                Debug.Log("<color=#FFFFFF>🐟 [Normal] 일반 품질(B등급)로 제압했습니다.</color>");
                fishingRod.CurrentHookedFish.Quality = FishQuality.GradeB;
                stateMachine.ChangeState(fishingRod.CaughtState);
            }
            else
            {
                // 점수가 30점 미만이면 낚시 실패!
                Debug.Log("<color=#FF5555>💥 [Fail] 완성도가 너무 낮아 물고기가 도망갔습니다!</color>");
                fishingRod.CurrentHookedFish = null;
                stateMachine.ChangeState(fishingRod.FailedState);
            }
        }

        private float CalculatePatternLength(EscapePatternDataSO dataSo)
        {
            if (dataSo == null || dataSo.Points == null || dataSo.Points.Count < 2) return 0f;
            float length = 0f;
            for (int i = 0; i < dataSo.Points.Count - 1; i++)
            {
                length += Vector2.Distance(dataSo.Points[i], dataSo.Points[i + 1]);
            }
            return length;
        }

        public override void Exit()
        {
            base.Exit();
            cts?.Cancel();
            cts?.Dispose();

            // [추가] 발악 패턴 프로퍼티 비활성화 (UI 오작동 및 노출 차단)
            fishingRod.IsStruggleActive.Value = false;

            // 나갈 때 선 및 도트 깔끔하게 지우기
            if (fishingRod.patternDrawer != null)
            {
                fishingRod.patternDrawer.ForceStopDrawing();
                fishingRod.patternDrawer.ClearAllDrawnLines();
                fishingRod.patternDrawer.enabled = false;
            }
            if (fishingRod.patternGenerator != null)
            {
                fishingRod.patternGenerator.ClearCurrentPattern();
            }
        }
    }
}