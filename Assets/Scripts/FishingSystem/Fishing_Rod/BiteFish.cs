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

        [Header("🎣 입질 물리 연출 설정")]
        [Tooltip("입질 순간 찌를 아래로 순간적으로 쳐박는 힘 (부력 이펙터에 의해 다시 솟구칩니다)")]
        [SerializeField] private float bitePlungeForce = 8f; 
        [SerializeField] private LayerMask fishingZoneLayer = ~0;
        [SerializeField] private float detectionRadius = 1.2f;

        private DisposableBag disposables;
        private CancellationTokenSource biteCts;
        
        // 현재 입질이 온 물고기의 실시간 런타임 데이터
        private FishData currentHookedFish;
        public FishData CurrentHookedFish => currentHookedFish;

        void Start()
        {
            if (fishingRod == null)
            {
                Debug.LogError("⚠️ BiteManager에 FishingRod가 연결되지 않았습니다!");
                return;
            }

            // 낚싯대의 찌 상태가 변경되는 것을 관찰합니다.
            fishingRod.BobberStateProperty
                .Subscribe(OnBobberStateChanged)
                .AddTo(ref disposables);
        }

        private void OnBobberStateChanged(BobberState state)
        {
            // 새로운 캐스팅이나 리셋이 일어나면 기존 입질 타이머는 무조건 취소
            CancelBiteTimer();

            if (state == BobberState.Settled)
            {
                // 찌가 물에 안착했다면 입질 프로세스 작동!
                biteCts = new CancellationTokenSource();
                StartBiteCountdownAsync(biteCts.Token).Forget();
            }
            else if (state == BobberState.Ready)
            {
                // 회수 상태라면 들고 있던 물고기 정보 파괴
                currentHookedFish = null;
            }
        }

        /// <summary>
        /// 찌가 머무는 구역의 FishingZone을 체크하고 입질이 올 때까지 대기하는 태스크
        /// </summary>
        private async UniTaskVoid StartBiteCountdownAsync(CancellationToken cancellationToken)
        {
            Transform bobber = fishingRod.Bobber;
            if (bobber == null) return;

            // 1. 찌 위치 기반으로 FishingZone 탐색
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

            if (currentZone == null)
            {
                Debug.LogWarning("⚠️ 찌가 위치한 곳에서 FishingZone을 찾지 못해 입질이 발생하지 않습니다.");
                return;
            }

            Debug.Log($"<color=cyan>📍 [{currentZone.zoneName}] 입질 타이머 가동 시작...</color>");

            try
            {
                // 2. 설정된 범위만큼 랜덤 대기
                float waitTime = Random.Range(biteDelayRange.x, biteDelayRange.y);
                await UniTask.Delay(System.TimeSpan.FromSeconds(waitTime), cancellationToken: cancellationToken);

                // 3. 해당 낚시터 구역에서 물고기 랜덤 추첨
                FishDataSO selectedData = currentZone.GetRandomFish();
                if (selectedData == null) return;

                // 4. 런타임 데이터 인스턴스 생성
                currentHookedFish = new FishData(selectedData);

                // 5. 최종 입질 실행!
                TriggerBite();
            }
            catch (System.OperationCanceledException)
            {
                // 대기 중 낚시줄을 감았거나 취소됨
            }
        }

        /// <summary>
        /// 실제 물고기가 바늘을 물었을 때의 물리 연출 및 로그 출력
        /// </summary>
        private void TriggerBite()
        {
            // 낚싯대의 줄 상태를 팽팽함(Taut)으로 변경 요구
            fishingRod.SetLineState(FishingLineState.Taut);

            // [물리 연출] 부력 이펙터 구역 안에서 찌를 아래로 순간 충격(Impulse)을 줘서 가라앉힘
            Rigidbody2D bobberRb = fishingRod.BobberRb;
            if (bobberRb != null)
            {
                bobberRb.linearVelocity = Vector2.zero; // 깔끔한 연출을 위해 기존 부력 속도 초기화
                bobberRb.AddForce(Vector2.down * bitePlungeForce, ForceMode2D.Impulse);
            }

            // 어떤 등급의 물고기가 물었는지 가독성 높은 폰트 컬러 로그 출력
            if (currentHookedFish != null)
            {
                string gradeColor = GetLogColorByGrade(currentHookedFish.Data.grade);
                Debug.Log($"<color={gradeColor}>🎯 [입질 발생!] 등급: {currentHookedFish.Data.grade} | 이름: {currentHookedFish.Data.fishName} | 최대 기력: {currentHookedFish.Data.maxStamina}</color>");
                Debug.Log($"<color=white>📊 스펙 -> 힘: {currentHookedFish.Data.strength}, 저항: {currentHookedFish.Data.resistance}, 민첩: {currentHookedFish.Data.agility}</color>");
            }
        }

        private void CancelBiteTimer()
        {
            biteCts?.Cancel();
            biteCts?.Dispose();
            biteCts = null;
        }

        private string GetLogColorByGrade(FishGrade grade)
        {
            return grade switch
            {
                FishGrade.Common => "#FFFFFF",  // 일반: 흰색
                FishGrade.Rare => "#00FFFF",    // 희귀: 하늘색
                FishGrade.Epic => "#A020F0",    // 에픽: 보라색
                FishGrade.Unique => "#FFA500",  // 유니크: 주황색
                FishGrade.Legend => "#FF0000",  // 전설: 빨간색
                _ => "#FFFFFF"
            };
        }

        void OnDestroy()
        {
            CancelBiteTimer();
            disposables.Dispose();
        }
    }
}