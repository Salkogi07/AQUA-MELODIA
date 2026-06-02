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
                return;
            }
            else
            {
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
                
                isProcessingBite = true; 
                fishingRod.SetBobberState(BobberState.Biting);

                bool isSuccess = await WaitForPlayerReactionAsync(cancellationToken);
                
                isProcessingBite = false; 

                if (isSuccess)
                {
                    OnCatchSuccess();
                }
                else
                {
                    OnCatchFailed();
                }
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                isProcessingBite = false;
            }
        }

        private async UniTask<bool> WaitForPlayerReactionAsync(CancellationToken cancellationToken)
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

                    return completedTaskIndex == 1; 
                }
                catch (System.OperationCanceledException) 
                {
                    return false;
                }
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
            Debug.Log($"<color=#00FF00>⚔️ [챔질 성공] 성공적인 챔질! 물고기가 걸렸습니다.</color>");
            
            // 💡 [여기에 새로운 이벤트 연결]
            // 예: FishingEventManager.Instance.StartFishingMiniGame(currentHookedFish);
            // 새로운 이벤트를 시작할 때 찌 상태(BobberState)를 미니게임용 커스텀 상태로 전환해 제어하시면 편합니다.

            // 현재는 임시로 즉시 자동 회수되도록 복구해 두었습니다.
            fishingRod.RetrieveBobberAsync().Forget();
        }

        private void OnCatchFailed()
        {
            Debug.Log($"<color=#AAAAAA>💨 [놓침] 플레이어가 놓쳐 물고기가 도망쳤습니다. 찌를 그대로 두고 재대기합니다.</color>");
            currentHookedFish = null;

            Rigidbody2D bobberRb = fishingRod.BobberRb;
            if (bobberRb != null) bobberRb.linearVelocity = Vector2.zero;

            // 낚시에 실패하면 휠을 감지 않고 그 자리에서 다시 입질 타이머 가동
            fishingRod.SetLineState(FishingLineState.Slack);
            fishingRod.SetBobberState(BobberState.Settled);
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