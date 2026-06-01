using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fish; 

namespace FishingSystem.Fishing_Rod
{
    public class BiteFish : MonoBehaviour
    {
        [Header("연결할 캐스팅 시스템")]
        [SerializeField] private FishingRod fishingRod;

        [Header("🐟 입질 타이머 설정")]
        [SerializeField] private Vector2 biteDelayRange = new Vector2(2f, 5f); 

        [Header("⏱️ 반응 제한 시간 설정")]
        [SerializeField] private float inputTimeLimit = 1.5f; 

        [Header("🎣 입질 물리 연출 및 탐색 설정")]
        [SerializeField] private float bitePlungeForce = 8f; 
        [SerializeField] private LayerMask fishingZoneLayer = ~0;
        [SerializeField] private float detectionRadius = 1.2f;

        private DisposableBag disposables; 
        private CancellationTokenSource biteCts;
        private FishData currentHookedFish;
        
        // 💡 입질 및 챔질 시퀀스가 내부적으로 진행 중인지 확인하는 플래그
        private bool isProcessingBite = false; 

        void Start()
        {
            if (fishingRod == null) return;

            fishingRod.BobberStateProperty
                .Subscribe(OnBobberStateChanged)
                .AddTo(ref disposables);
        }

        private void OnBobberStateChanged(BobberState state)
        {
            if (state == BobberState.Settled)
            {
                CancelBiteTimer();
                biteCts = new CancellationTokenSource();
                StartBiteCountdownAsync(biteCts.Token).Forget();
            }
            else if (state == BobberState.Biting)
            {
                // 💡 찌 상태가 Biting으로 변한 것은 입질이 시작되었다는 뜻이므로 타이머를 취소하지 않고 무시합니다.
                return;
            }
            else
            {
                // Ready, Flying, Retrieving 상태로 변경되었을 때
                // 💡 챔질 성공/실패로 인한 자동 회수가 아니라, 플레이어가 '대기 도중 임의로' 감아올렸을 때만 타이머를 취소합니다.
                if (!isProcessingBite)
                {
                    CancelBiteTimer();
                    currentHookedFish = null;
                }
            }
        }

        private async UniTaskVoid StartBiteCountdownAsync(CancellationToken cancellationToken)
        {
            Transform bobber = fishingRod.Bobber;
            if (bobber == null) return;

            FishingZone currentZone = null;
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(bobber.position, detectionRadius, fishingZoneLayer);
            
            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<FishingZone>(out var zone))
                {
                    currentZone = zone;
                    break;
                }
            }

            if (currentZone == null) return;

            Debug.Log($"<color=cyan>📍 [{currentZone.zoneName}] 입질 대기 시작...</color>");

            try
            {
                float waitTime = Random.Range(biteDelayRange.x, biteDelayRange.y);
                await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: cancellationToken);

                FishDataSO selectedData = currentZone.GetRandomFish();
                if (selectedData == null) return;

                currentHookedFish = new FishData(selectedData);
                
                // 💡 상태 변경 전에 플래그를 활성화하여 OnBobberStateChanged에서의 자가 취소를 방지합니다.
                isProcessingBite = true; 
                fishingRod.SetBobberState(BobberState.Biting);

                await WaitForPlayerReactionAsync(cancellationToken);
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                // 💡 성공하든 실패하든 시퀀스가 완전히 종료되면 플래그를 안전하게 해제합니다.
                isProcessingBite = false;
            }
        }

        private async UniTask WaitForPlayerReactionAsync(CancellationToken cancellationToken)
        {
            TriggerBitePhysics();
            
            Debug.Log($"<color=yellow>⏰ 찌가 들어갔습니다! {inputTimeLimit}초 안에 [마우스 좌클릭]으로 챔질하세요!!</color>");

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    var clickTask = UniTask.WaitUntil(() => Input.GetMouseButtonDown(0), cancellationToken: linkedCts.Token);
                    var timeoutTask = UniTask.Delay(System.TimeSpan.FromSeconds(inputTimeLimit), cancellationToken: linkedCts.Token);

                    int completedTaskIndex = await UniTask.WhenAny(timeoutTask, clickTask);
                    linkedCts.Cancel();

                    if (completedTaskIndex == 1)
                    {
                        OnCatchSuccess();
                    }
                    else
                    {
                        OnCatchFailed();
                    }
                }
                catch (System.OperationCanceledException) { }
            }
        }

        private void TriggerBitePhysics()
        {
            fishingRod.SetLineState(FishingLineState.Taut);
            Rigidbody2D bobberRb = fishingRod.BobberRb;
            if (bobberRb != null)
            {
                bobberRb.linearVelocity = Vector2.zero; 
                bobberRb.AddForce(Vector2.down * bitePlungeForce, ForceMode2D.Impulse);
            }

            if (currentHookedFish != null)
            {
                Debug.Log($"<color=red>🎯 [물고기 물음!] 이름: {currentHookedFish.Data.fishName}</color>");
            }
        }

        private void OnCatchSuccess()
        {
            Debug.Log($"<color=#00FF00>⚔️ [낚시 성공] 성공적인 챔질! 물고기 {currentHookedFish.Data.fishName}(을)를 낚아 올렸습니다!</color>");
            //fishingRod.RetrieveBobberAsync().Forget();
        }

        private void OnCatchFailed()
        {
            Debug.Log($"<color=#AAAAAA>💨 [놓침] 플레이어 반응 지연으로 물고기가 도망쳤습니다.</color>");
            currentHookedFish = null;
            //fishingRod.RetrieveBobberAsync().Forget();
        }

        private void CancelBiteTimer()
        {
            biteCts?.Cancel();
            biteCts?.Dispose();
            biteCts = null;
        }

        void OnDestroy()
        {
            CancelBiteTimer();
            disposables.Dispose();
        }
    }
}