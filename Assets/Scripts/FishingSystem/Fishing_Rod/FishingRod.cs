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
        [Range(0f, 100f)] 
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
            if (bobberState.Value == BobberState.Ready && bobber != null)
            {
                bobber.position = GetTargetStartPosition();
            }

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
                    CastBobberAsync().Forget();
                    break;

                case BobberState.Flying:
                case BobberState.Settled:
                    // 일반 상황 혹은 허공 클릭 시 자동 회수
                    RetrieveBobberAsync().Forget();
                    break;

                case BobberState.Biting:
                    // 입질 중일 때는 BiteFish 스크립트가 챔질 입력을 처리함
                    break;

                case BobberState.Retrieving:
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
        /// 찌를 낚싯대 방향으로 회수합니다.
        /// </summary>
        public async UniTaskVoid RetrieveBobberAsync()
        {
            if (bobber == null || bobberRb == null) return;

            CleanCancellationToken();
            castCts = new CancellationTokenSource();

            bobberState.Value = BobberState.Retrieving;
            lineState.Value = FishingLineState.Taut; 

            bobberRb.bodyType = RigidbodyType2D.Dynamic; 
            
            Debug.Log("<color=#99E6FF>🎣 릴을 감아 찌를 회수합니다...</color>");

            try
            {
                while (true)
                {
                    Vector3 targetPos = GetTargetStartPosition();
                    float distance = Vector3.Distance(bobber.position, targetPos);

                    if (distance < 0.6f) break;

                    Vector2 pullDirection = (targetPos - bobber.position).normalized;
                    bobberRb.linearVelocity = pullDirection * reelInSpeed;

                    await UniTask.Yield(PlayerLoopTiming.Update, castCts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                return; 
            }

            Debug.Log("<color=white>📥 찌 회수 완료. 다음 캐스팅 대기.</color>");
            ResetBobberToReady();
        }

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