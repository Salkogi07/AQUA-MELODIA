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

        private Rigidbody2D bobberRb;
        private CancellationTokenSource castCts;
        private bool isCasted = false;

        // R3: 상태 관리 (줄 상태 & 찌의 물리적 위치 상태)
        private readonly ReactiveProperty<FishingLineState> lineState = new(FishingLineState.Slack);
        public ReadOnlyReactiveProperty<FishingLineState> LineState => lineState;

        private readonly ReactiveProperty<BobberState> bobberState = new(BobberState.Ready);
        public ReadOnlyReactiveProperty<BobberState> BobberStateProperty => bobberState;

        public Transform RodTip => rodTip;
        public Transform Bobber => bobber;
        public Rigidbody2D BobberRb => bobberRb; // 입질 시스템이 물리 연출을 할 수 있도록 개방

        void Start()
        {
            if (bobber != null) bobberRb = bobber.GetComponent<Rigidbody2D>();
            if (fishingLine != null) fishingLine.Initialize(this);
            ResetBobberToReady();
        }

        void Update()
        {
            if (!isCasted && bobber != null)
            {
                bobber.position = GetTargetStartPosition();
            }
        }

        public void ResetBobberToReady()
        {
            isCasted = false;
            lineState.Value = FishingLineState.Slack;
            bobberState.Value = BobberState.Ready; // 상태 리셋

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

            castCts?.Cancel();
            castCts?.Dispose();
            castCts = new CancellationTokenSource();

            isCasted = true;
            bobber.position = GetTargetStartPosition();

            bobberRb.bodyType = RigidbodyType2D.Dynamic;
            bobberRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            Vector2 launchVelocity = castDirection.normalized * castSpeed;
            bobberRb.linearVelocity = launchVelocity;
            bobberRb.angularVelocity = 0f;

            lineState.Value = FishingLineState.Slack;
            bobberState.Value = BobberState.Flying; // 날아가는 중

            Debug.Log($"<color=lime>🚀 캐스팅 발사! 속도: {castSpeed}</color>");

            try
            {
                // 찌가 날아가다가 물에 안착해서 완전히 멈출 때까지 대기
                await UniTask.WaitUntil(() => bobberRb.linearVelocity.magnitude < 0.2f, cancellationToken: castCts.Token);
                
                Debug.Log("<color=yellow>🌊 찌 안착 완료!</color>");
                bobberState.Value = BobberState.Settled; // 안착 상태 알림 -> 입질 매니저가 이 신호를 감지합니다.
            }
            catch (System.OperationCanceledException)
            {
                // 캐스팅 취소 시 처리
            }
        }

        // 입질 시스템 등 외부 시스템이 낚싯줄의 상태를 제어할 수 있도록 메서드 제공
        public void SetLineState(FishingLineState state)
        {
            lineState.Value = state;
        }

        private Vector3 GetTargetStartPosition()
        {
            return castStartPosition != null ? castStartPosition.position : rodTip.position;
        }

        void OnDestroy()
        {
            castCts?.Cancel();
            castCts?.Dispose();
            lineState.Dispose();
            bobberState.Dispose();
        }
    }
}