using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fishing_Rod;

namespace FishingSystem.FishState
{
    public class RetrievingState : FishingState
    {
        private CancellationTokenSource cts;
        private bool isPulled = false;

        public RetrievingState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter(); // "IsRetrieving" 애니메이션 시작
            isPulled = false;
            cts = new CancellationTokenSource();
            
            // 줄을 팽팽하게 만듭니다.
            fishingRod.SetLineState(FishingLineState.Taut);
            
            Debug.Log("<color=#99E6FF>🎣 회수 시작: 줄이 당겨지며 찌가 서서히 끌려옵니다...</color>");
        }

        public override void Update()
        {
            if (!isPulled)
            {
                // 찌가 낚싯대 끝 방향으로 스르륵 끌려오는 연출 (관성 유지 + 약한 당김)
                Vector2 directionToRod = (fishingRod.rodTip.position - fishingRod.bobber.position).normalized;
                
                // 기존 속도에서 낚싯대 방향으로 서서히 방향과 속도를 변환 (숫자 4f는 끌려오는 예비 속도)
                fishingRod.BobberRb.linearVelocity = Vector2.Lerp(fishingRod.BobberRb.linearVelocity, directionToRod * 4f, Time.deltaTime * 5f);
            }
        }

        // 애니메이션 이벤트에서 호출됨
        public void ExecutePull()
        {
            if (isPulled) return;
            isPulled = true;
            
            // 날아오기 시작할 때 물리를 끄고(Kinematic), 속도를 0으로 초기화
            fishingRod.ResetBobberPhysics();
            
            Debug.Log("<color=#99E6FF>🚀 낚싯대를 획 당겼습니다! 찌가 손으로 날아옵니다.</color>");
            PullBobberRoutineAsync(cts.Token).Forget();
        }

        private async UniTaskVoid PullBobberRoutineAsync(CancellationToken token)
        {
            Transform bobber = fishingRod.bobber;
            // 손 위치가 없다면 낚싯대 끝으로 임시 지정
            Transform targetHand = fishingRod.catchHandPosition != null ? fishingRod.catchHandPosition : fishingRod.rodTip;
            
            Vector3 startPos = bobber.position;
            float timeElapsed = 0f;
            float duration = fishingRod.retrieveDuration;
            
            try
            {
                // 포물선 비행 로직
                while (timeElapsed < duration)
                {
                    timeElapsed += Time.deltaTime;
                    float t = timeElapsed / duration; 
                    
                    // 2차 베지어 곡선 (포물선 계산)
                    Vector3 p0 = startPos;
                    Vector3 p2 = targetHand.position; // 움직이는 플레이어를 따라가도록 매 프레임 갱신
                    
                    // 중간 지점(p1)을 위로 띄워서 포물선을 만듦
                    Vector3 p1 = (p0 + p2) / 2f + (Vector3.up * fishingRod.retrieveArcHeight); 
                    
                    float u = 1 - t;
                    Vector3 currentPos = (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
                    
                    bobber.position = currentPos;
                    
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                
                // 도착 완료
                bobber.position = targetHand.position;
                Debug.Log("<color=white>🖐️ 찌를 손으로 잡았습니다! 회수 완료.</color>");
                
                // 대기 상태로 복귀
                stateMachine.ChangeState(fishingRod.ReadyState);
            }
            catch (System.OperationCanceledException) 
            {
                // 중간에 상태가 바뀌어 취소된 경우 무시
            }
        }

        public override void Exit()
        {
            base.Exit();
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}