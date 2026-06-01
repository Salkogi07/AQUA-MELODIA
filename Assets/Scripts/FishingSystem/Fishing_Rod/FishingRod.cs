using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace FishingSystem.Fishing_Rod
{
    public class FishingRod : MonoBehaviour
    {
        [Header("연결할 하위 컴포넌트")]
        [SerializeField] private FishingLine fishingLine;

        [Header("연결할 오브젝트")]
        [SerializeField] private Transform rodTip;           
        [SerializeField] private Transform bobber;           
        [SerializeField] private Transform castStartPosition; 

        [Header("캐스팅(속도) 설정")]
        [SerializeField] private Vector2 castDirection = new Vector2(1.2f, 1f); 
        [Range(0f, 100f)] 
        [SerializeField] private float castSpeed = 25f;                       

        [Header("🎣 회수(릴링) 설정")]
        [Tooltip("찌를 다시 감아올릴 때의 속도입니다.")]
        [SerializeField] private float reelInSpeed = 18f;

        private Rigidbody2D bobberRb;
        private CancellationTokenSource castCts;
        private bool isCasted = false;

        private readonly ReactiveProperty<FishingLineState> lineState = new(FishingLineState.Slack);
        public ReadOnlyReactiveProperty<FishingLineState> LineState => lineState;

        private readonly ReactiveProperty<BobberState> bobberState = new(BobberState.Ready);
        public ReadOnlyReactiveProperty<BobberState> BobberStateProperty => bobberState;

        public Transform RodTip => rodTip;
        public Transform Bobber => bobber;
        public Rigidbody2D BobberRb => bobberRb; 

        void Start()
        {
            if (bobber != null) bobberRb = bobber.GetComponent<Rigidbody2D>();
            if (fishingLine != null) fishingLine.Initialize(this);
            ResetBobberToReady();
        }

        void Update()
        {
            // 던지기 전 대기 상태일 때만 낚싯대 끝에 고정
            if (bobberState.Value == BobberState.Ready && bobber != null)
            {
                bobber.position = GetTargetStartPosition();
            }

            // 🖱️ 통합 마우스 좌클릭 입력 처리
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseInput();
            }
        }

        private void HandleMouseInput()
        {
            switch (bobberState.Value)
            {
                case BobberState.Ready:
                    // 1. 대기 상태일 때 클릭하면 던지기
                    CastBobberAsync().Forget();
                    break;

                case BobberState.Flying:
                case BobberState.Settled:
                    // 2. 날아가는 중이거나 물에 떠 있을 때 클릭하면 즉시 회수 시작
                    RetrieveBobberAsync().Forget();
                    break;

                case BobberState.Biting:
                    // 3. 물고기가 물었을 때는 BiteManager가 전권을 가지고 타이밍을 체크하므로 여기선 무시합니다.
                    break;

                case BobberState.Retrieving:
                    // 4. 이미 회수 중일 때는 중복 입력을 무시합니다.
                    break;
            }
        }

        public void ResetBobberToReady()
        {
            isCasted = false;
            lineState.Value = FishingLineState.Slack;
            bobberState.Value = BobberState.Ready; 

            if (bobber != null) bobber.position = GetTargetStartPosition();

            if (bobberRb != null)
            {
                bobberRb.bodyType = RigidbodyType2D.Kinematic;
                bobberRb.linearVelocity = Vector2.zero;
                bobberRb.angularVelocity = 0f;
            }
        }

        public async UniTaskVoid CastBobberAsync()
        {
            if (bobber == null || bobberRb == null) return;

            CleanCancellationToken();
            castCts = new CancellationTokenSource();

            isCasted = true;
            bobber.position = GetTargetStartPosition();
            bobberRb.bodyType = RigidbodyType2D.Dynamic;

            Vector2 launchVelocity = castDirection.normalized * castSpeed;
            bobberRb.linearVelocity = launchVelocity;
            bobberRb.angularVelocity = 0f;

            lineState.Value = FishingLineState.Slack;
            bobberState.Value = BobberState.Flying; 

            Debug.Log($"<color=lime>🚀 캐스팅 발사! 속도: {castSpeed}</color>");

            try
            {
                await UniTask.WaitUntil(() => bobberRb.linearVelocity.magnitude < 0.2f, cancellationToken: castCts.Token);
                
                Debug.Log("<color=yellow>🌊 찌 안착 완료!</color>");
                bobberState.Value = BobberState.Settled; 
            }
            catch (System.OperationCanceledException) { }
        }

        /// <summary>
        /// 🎣 [신규] 찌를 플레이어 방향으로 자연스럽게 당겨오는 물리 회수 로직
        /// </summary>
        public async UniTaskVoid RetrieveBobberAsync()
        {
            if (bobber == null || bobberRb == null) return;

            CleanCancellationToken();
            castCts = new CancellationTokenSource();

            bobberState.Value = BobberState.Retrieving;
            lineState.Value = FishingLineState.Taut; // 감아올릴 때는 줄을 팽팽하게 세팅

            bobberRb.bodyType = RigidbodyType2D.Dynamic; // 부력과 물리 마찰을 받도록 Dynamic 유지
            
            Debug.Log("<color=#99E6FF>🎣 릴을 감아 찌를 회수합니다...</color>");

            try
            {
                while (true)
                {
                    Vector3 targetPos = GetTargetStartPosition();
                    float distance = Vector3.Distance(bobber.position, targetPos);

                    // 낚싯대 끝부분에 충분히 도달하면 회수 완료 탈출
                    if (distance < 0.6f) break;

                    // 플레이어 위치(낚싯대 끝) 방향으로 실시간 물리 속도 부여 (물 위를 스치듯 끌려옴)
                    Vector2 pullDirection = (targetPos - bobber.position).normalized;
                    bobberRb.linearVelocity = pullDirection * reelInSpeed;

                    await UniTask.Yield(PlayerLoopTiming.Update, castCts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                return; // 중간에 다른 상태로 캔슬 시 종료
            }

            Debug.Log("<color=white>📥 찌 회수 완료. 다음 캐스팅 대기.</color>");
            ResetBobberToReady();
        }

        // 외부에서 찌 상태를 강제로 바꿀 수 있도록 개방 (BiteManager용)
        public void SetBobberState(BobberState state)
        {
            bobberState.Value = state;
        }

        public void SetLineState(FishingLineState state)
        {
            lineState.Value = state;
        }

        private Vector3 GetTargetStartPosition()
        {
            return castStartPosition != null ? castStartPosition.position : rodTip.position;
        }

        private void CleanCancellationToken()
        {
            castCts?.Cancel();
            castCts?.Dispose();
            castCts = null;
        }

        void OnDestroy()
        {
            CleanCancellationToken();
            lineState.Dispose();
            bobberState.Dispose();
        }
    }
}