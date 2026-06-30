using UnityEngine;
using R3;
using FishingSystem.Fishing_Rod;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class FishingRod : MonoBehaviour
    {
        [Header("연결할 하위 컴포넌트")]
        public FishingLine fishingLine;
        public Transform rodTip;
        public Transform bobber;
        public Transform castStartPosition;

        [Header("캐스팅(속도) 설정")]
        public Vector2 castDirection = new Vector2(1.2f, 1f);
        [Range(0f, 100f)] public float castSpeed = 25f;

        [Header("🎣 회수(릴링) 설정")]
        [Range(0f, 100f)] public float reelInSpeed = 18f;

        [Header("🐟 입질 타이머 설정")]
        public Vector2 biteDelayRange = new Vector2(2f, 5f);
        public float inputTimeLimit = 1.5f;

        [Header("🎣 입질 물리 연출 및 탐색 설정")]
        public float bitePlungeForce = 8f;
        public LayerMask fishingZoneLayer = ~0;
        public float detectionRadius = 1.2f;

        // --- 컴포넌트 프로퍼티 ---
        public Rigidbody2D BobberRb { get; private set; }
        
        // --- 낚싯줄 상태 프로퍼티 (R3) ---
        private readonly ReactiveProperty<FishingLineState> lineState = new(FishingLineState.Slack);
        public ReadOnlyReactiveProperty<FishingLineState> LineState => lineState;

        // --- 상태 머신 및 상태들 ---
        public FishingStateMachine StateMachine { get; private set; }
        public ReadyState ReadyState { get; private set; }
        public CastingState CastingState { get; private set; }
        public SettledState SettledState { get; private set; }
        public BitingState BitingState { get; private set; }
        public RetrievingState RetrievingState { get; private set; }

        public FishData CurrentHookedFish { get; set; }

        private void Awake()
        {
            if (bobber != null) BobberRb = bobber.GetComponent<Rigidbody2D>();

            // 상태 초기화 (애니메이션 파라미터가 없다면 빈 문자열 전달)
            StateMachine = new FishingStateMachine();
            ReadyState = new ReadyState(this, StateMachine, "IsReady");
            CastingState = new CastingState(this, StateMachine, "IsCasting");
            SettledState = new SettledState(this, StateMachine, "IsSettled");
            BitingState = new BitingState(this, StateMachine, "IsBiting");
            RetrievingState = new RetrievingState(this, StateMachine, "IsRetrieving");
        }

        private void Start()
        {
            if (fishingLine != null) fishingLine.Initialize(this); // 낚시줄 초기화
            StateMachine.Initialize(ReadyState);
        }

        private void Update()
        {
            StateMachine.CurrentState?.Update();
        }

        private void FixedUpdate()
        {
            StateMachine.CurrentState?.FixedUpdate();
        }

        // ==========================================
        // 상태(State)에서 가져다 쓸 핵심 동작(기능) 함수들
        // ==========================================

        public void SetLineState(FishingLineState state)
        {
            lineState.Value = state;
        }

        public Vector3 GetTargetStartPosition()
        {
            return castStartPosition != null ? castStartPosition.position : rodTip.position;
        }
        
        public void StopBobberMovement()
        {
            if (BobberRb == null) return;
            BobberRb.linearVelocity = Vector2.zero;
            BobberRb.angularVelocity = 0f;
        }

        public void ResetBobberPhysics()
        {
            if (BobberRb == null) return;
            BobberRb.bodyType = RigidbodyType2D.Kinematic;
            BobberRb.linearVelocity = Vector2.zero;
            BobberRb.angularVelocity = 0f;
        }

        public void ApplyCastPhysics()
        {
            if (BobberRb == null) return;
            BobberRb.bodyType = RigidbodyType2D.Dynamic;
            Vector2 launchVelocity = castDirection.normalized * castSpeed;
            BobberRb.linearVelocity = launchVelocity;
            BobberRb.angularVelocity = 0f;
        }

        public void ApplyBitePhysics()
        {
            if (BobberRb != null)
            {
                BobberRb.bodyType = RigidbodyType2D.Dynamic;
                
                BobberRb.linearVelocity = Vector2.zero;
                BobberRb.AddForce(Vector2.down * bitePlungeForce, ForceMode2D.Impulse);
            }
        }

        public FishDataSO SearchFishingZone()
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(bobber.position, detectionRadius, fishingZoneLayer);
            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<FishingZone>(out var zone))
                {
                    return zone.GetRandomFish();
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            lineState.Dispose();
        }
        
        public void OnAnimationEvent_ThrowBobber()
        {
            // 현재 상태가 CastingState일 때만 실제 찌를 날리도록 안전장치 처리
            if (StateMachine.CurrentState == CastingState)
            {
                CastingState.ExecuteCast();
            }
        }
    }
}