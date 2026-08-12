using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Input_Helper;
using FishingSystem.Data;

namespace FishingSystem.FishState
{
    public class ShowcaseState : FishingState
    {
        private GameObject fishObject;
        private bool isRevealed = false; // 물고기가 화면에 나타났는지 여부
        private CancellationTokenSource cts;

        // 물고기가 원래 가지고 있던 스케일을 저장하는 변수
        private Vector3 originalScale = Vector3.one; 

        public ShowcaseState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public void SetFishObject(GameObject obj) => fishObject = obj;

        public override void Enter()
        {
            isRevealed = false;
            cts = new CancellationTokenSource();
            
            // 찌 비활성화 및 낚싯줄 숨김
            fishingRod.bobber.gameObject.SetActive(false);
            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.None);
            
            float cameraDuration = 1f;
            fishingRod.showcaseCameraPoint.PlayCameraAction(cameraDuration);

            if (fishObject != null)
            {
                fishObject.transform.SetParent(fishingRod.showcaseMountPoint);
                fishObject.transform.localPosition = Vector3.zero;
                fishObject.transform.localRotation = Quaternion.identity;
                
                // 물고기가 생성될 당시 혹은 프리팹 고유의 원래 크기를 기억합니다.
                originalScale = fishObject.transform.localScale;

                // 스케일을 0으로 만들어 애니메이션 시작 전까지 숨겨둡니다.
                fishObject.transform.localScale = Vector3.zero;
                fishObject.gameObject.SetActive(false);
            }

            // 비동기로 카메라 연출 시간을 대기한 후 애니메이션을 재생합니다.
            WaitCameraAndPlayAnimationAsync(cameraDuration, cts.Token).Forget();
        }

        // 카메라 이동이 끝날 때까지 대기한 후 캐릭터 애니메이션을 트리거하는 루틴
        private async UniTaskVoid WaitCameraAndPlayAnimationAsync(float delaySeconds, CancellationToken token)
        {
            try
            {
                // 지정된 카메라 연출 시간만큼 대기
                await UniTask.Delay(System.TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
                
                // 대기 완료 후 캐릭터의 자랑하기 애니메이션 작동 (IsShowcase = true)
                base.Enter();
                
                Debug.Log("<color=orange>🎬 카메라 연출 완료. 캐릭터 자랑하기 애니메이션을 시작합니다!</color>");
            }
            catch (System.OperationCanceledException)
            {
                // 대기 중 상태가 취소된 경우 예외 처리
            }
        }

        // 애니메이션 이벤트에 의해 호출되는 함수
        public void RevealFish()
        {
            if (isRevealed || fishObject == null) return;
            isRevealed = true;

            fishObject.gameObject.SetActive(true);
            PopUpFishVisualAsync().Forget();

            // 물고기를 펼쳐 자랑할 때 화면 텍스트 UI 렌더링
            fishingRod.ShowcaseFish.Value = fishingRod.CurrentHookedFish;
            
            if (FishChest.Instance != null)
            {
                FishChest.Instance.PlayShakeEffect();
            }
        }

        // 물고기가 뿅 하고 나타나는 스케일 보간 애니메이션
        private async UniTaskVoid PopUpFishVisualAsync()
        {
            float elapsed = 0f;
            float duration = 0.25f; // 나타나는 시간 (0.25초)

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 이징 함수(EaseOutBack)를 이용해 원래 스케일의 1.15배까지 커졌다가 1배로 쫀득하게 맞춰지는 연출
                float scaleMultiplier = Mathf.Sin(t * Mathf.PI * 0.5f); 
                if (t < 0.8f) scaleMultiplier *= 1.15f; 

                if (fishObject == null) return;
                
                fishObject.transform.localScale = originalScale * scaleMultiplier;

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 애니메이션이 완료되면 오차 없이 정확한 원래 크기로 고정합니다.
            if (fishObject != null) fishObject.transform.localScale = originalScale;
        }

        public override void Update()
        {
            if (isRevealed && FishingInput.GetLeftClickDown())
            {
                stateMachine.ChangeState(fishingRod.ReadyState);
            }
        }

        public override void Exit()
        {
            cts?.Cancel();
            cts?.Dispose();

            base.Exit();
            if (fishObject != null) Object.Destroy(fishObject);
            
            fishingRod.ShowcaseFish.Value = null;

            fishingRod.bobber.gameObject.SetActive(true);
            fishingRod.CurrentHookedFish = null;
            isRevealed = false;
            CameraManager.Instance.ResetCamera(.8f);
        }
    }
}