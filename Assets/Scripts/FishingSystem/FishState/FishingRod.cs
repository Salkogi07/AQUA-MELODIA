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
        
        [Header("🎮 미니게임 규칙 및 릴(Reel) 설정")]
        [Tooltip("마우스 휠 1틱당 이동하는 조준점 속도")]
        public float wheelSensitivity = 0.05f; 
        [Tooltip("물고기 위치와 내 조준점 위치의 허용 오차 (0.1 = 10%)")]
        public float sweetSpotTolerance = 0.1f; 
        
        [Tooltip("물고기 체력(기력)이 깎이는 속도 (초당)")]
        public float fishDamageRate = 15f;
        [Tooltip("오차범위를 벗어났을 때 스트레스(텐션)가 오르는 속도 (0~1 기준)")]
        public float stressIncreaseRate = 0.2f; 
        [Tooltip("오차범위를 맞췄을 때 스트레스(텐션)가 내려가는 속도 (0~1 기준)")]
        public float stressDecreaseRate = 0.3f; 

        [Header("미니게임 좌표 매핑 (UI용)")]
        [Tooltip("패턴에서 사용하는 X좌표의 최솟값 (예: -5)")]
        public float patternMinX = -5f;
        [Tooltip("패턴에서 사용하는 X좌표의 최댓값 (예: 5)")]
        public float patternMaxX = 5f;

        // --- 컴포넌트 프로퍼티 ---
        public Rigidbody2D BobberRb { get; private set; }
        
        // --- 낚싯줄 상태 프로퍼티 (R3) ---
        private readonly ReactiveProperty<FishingLineState> lineState = new(FishingLineState.Slack);
        public ReadOnlyReactiveProperty<FishingLineState> LineState => lineState;
        
        public ReactiveProperty<float> FishUiRatio { get; private set; } = new(0.5f); // 물고기 위치 (0~1)
        public ReactiveProperty<float> PlayerReelRatio { get; private set; } = new(0.5f); // 플레이어 릴 위치 (0~1)
        public ReactiveProperty<float> LineStress { get; private set; } = new(0f); // 줄의 스트레스/텐션 (1이 되면 끊어짐)
        public ReactiveProperty<float> FishHpRatio { get; private set; } = new(1f); // 물고기 남은 체력 비율
        public ReactiveProperty<bool> IsMiniGameActive { get; private set; } = new(false); // 미니게임 진행 여부 (UI 끄고 켜기 용도)

        // --- 상태 머신 및 상태들 ---
        public FishingStateMachine StateMachine { get; private set; }
        public ReadyState ReadyState { get; private set; }
        public CastingState CastingState { get; private set; }
        public SettledState SettledState { get; private set; }
        public BitingState BitingState { get; private set; }
        public RetrievingState RetrievingState { get; private set; }
        public MiniGameState MiniGameState { get; private set; }
        public FailedState FailedState { get; private set; } 

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
            MiniGameState = new MiniGameState(this, StateMachine, "IsMiniGame");
            FailedState = new FailedState(this, StateMachine, "IsFailed"); 
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
        
        public void StartFinalStrugglePattern()
        {
            Debug.Log("<color=red>🔥 [발악 패턴 시작] 물고기의 마지막 저항이 시작됩니다!!</color>");
            // TODO: 발악 패턴 로직 또는 상태 연결
            
            // 임시로 회수 상태로 넘김
            StateMachine.ChangeState(RetrievingState);
        }

        private void OnDestroy()
        {
            lineState.Dispose();
            FishUiRatio.Dispose();
            PlayerReelRatio.Dispose();
            LineStress.Dispose();
            FishHpRatio.Dispose();
            IsMiniGameActive.Dispose();
        }
        
        
        public void OnAnimationEvent_ThrowBobber()
        {
            // 현재 상태가 CastingState일 때만 실제 찌를 날리도록 안전장치 처리
            if (StateMachine.CurrentState == CastingState)
            {
                CastingState.ExecuteCast();
            }
        }
        
        public void OnAnimationEvent_FailFinished()
        {
            // 현재 상태가 FailedState일 때만 발동 (안전장치)
            if (StateMachine.CurrentState == FailedState)
            {
                Debug.Log("<color=white>🔄 실패 연출 종료. 기본 대기 상태로 복귀합니다.</color>");
                StateMachine.ChangeState(ReadyState);
            }
        }
    }
}