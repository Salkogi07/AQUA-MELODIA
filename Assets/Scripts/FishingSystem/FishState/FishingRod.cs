using UnityEngine;
using R3;
using FishingSystem.Fishing_Rod;
using FishingSystem.Fish;
using FishingSystem.Fishing_Pattern;
using FishingSystem.Equipment;

namespace FishingSystem.FishState
{
    public class FishingRod : MonoBehaviour
    {
        [Header("연결할 하위 컴포넌트")]
        public FishingLine fishingLine;
        public Transform rodTip;
        public Transform bobber;
        public Transform castStartPosition;
        
        [Header("🔌 장비 연동")]
        public PlayerFishingEquipment playerEquipment;

        [Header("캐스팅(속도) 설정")]
        public Vector2 castDirection = new Vector2(1.2f, 1f);
        [Range(0f, 100f)] public float castSpeed = 25f;

        [Header("🚀 캐스팅 물리 세부 설정")]
        [Tooltip("공기 저항 (찌가 공중에서 비행할 때 비행 속도가 감속되는 비율)")]
        public float castAirDamping = 1.1f;
        [Tooltip("게이지 충전 속도")]
        public float chargeSpeed = 1.5f;

        [Header("🖐️ 회수(손으로 잡기) 설정")]
        public Transform catchHandPosition; 
        [Tooltip("찌가 날아올 때의 포물선 최고 높이")]
        public float retrieveArcHeight = 2.5f;
        [Tooltip("찌가 손으로 날아오는데 걸리는 시간")]
        public float retrieveDuration = 0.4f;

        [Header("🐟 입질 타이머 설정")]
        public Vector2 biteDelayRange = new Vector2(2f, 5f);
        public float inputTimeLimit = 1.5f;

        [Header("🎣 입질 물리 연출 및 탐색 설정")]
        public float bitePlungeForce = 8f;
        public LayerMask fishingZoneLayer = ~0;
        public float detectionRadius = 1.2f;
        
        [Header("💥 실패 연출")]
        [Tooltip("줄이 끊어질 때 재생할 픽셀 파티클 프리팹")]
        public GameObject snapParticlePrefab;
        
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

        [Header("🖌️ 발악 패턴(선 그리기) 시스템")]
        public CameraTargetPoint patternCameraPoint;
        public PatternGenerator patternGenerator;
        public PatternEvaluator patternEvaluator;
        public PatternDrawer patternDrawer;
        
        [Header("🏆 성공 연출 설정")]
        public Transform fishHookPoint; 
        public GameObject fishVisualPrefab; 
        public Transform showcaseMountPoint; 
        public CameraTargetPoint showcaseCameraPoint;

        public float DefaultGravity { get; private set; }

        // --- 컴포넌트 프로퍼티 ---
        public Rigidbody2D BobberRb { get; private set; }
        
        // --- 낚싯줄 상태 프로퍼티 (R3) ---
        private readonly ReactiveProperty<FishingLineState> lineState = new(FishingLineState.Slack);
        public ReadOnlyReactiveProperty<FishingLineState> LineState => lineState;
        
        public ReactiveProperty<float> FishUiRatio { get; private set; } = new(0.5f); 
        public ReactiveProperty<float> PlayerReelRatio { get; private set; } = new(0.5f); 
        public ReactiveProperty<float> LineStress { get; private set; } = new(0f); 
        public ReactiveProperty<float> FishHpRatio { get; private set; } = new(1f); 
        public ReactiveProperty<bool> IsMiniGameActive { get; private set; } = new(false); 

        // 캐스팅 충전 프로퍼티 (R3)
        public ReactiveProperty<float> CastPower { get; private set; } = new(0f);
        public ReactiveProperty<bool> IsCharging { get; private set; } = new(false);
        
        public ReactiveProperty<bool> IsStruggleActive { get; private set; } = new(false);
        public ReactiveProperty<float> StruggleInkRatio { get; private set; } = new(1f);
        
        public ReactiveProperty<FishData> ShowcaseFish { get; private set; } = new(null);

        // --- 낚시터 매핑 좌표계 ---
        public float patternMinX { get; private set; }
        public float patternMaxX { get; private set; }
        public float currentZoneCenterX { get; private set; }

        // 감지된 현재 활성 낚시터 정보
        public FishingZone ActiveFishingZone { get; set; }

        // --- 상태 머신 및 상태들 ---
        public FishingStateMachine StateMachine { get; private set; }
        public ReadyState ReadyState { get; private set; }
        public CastingState CastingState { get; private set; }
        public SettledState SettledState { get; private set; }
        public BitingState BitingState { get; private set; }
        public RetrievingState RetrievingState { get; private set; }
        public MiniGameState MiniGameState { get; private set; }
        public FailedState FailedState { get; private set; } 
        public FinalStruggleState FinalStruggleState { get; private set; }
        public CaughtState CaughtState { get; private set; }
        public ShowcaseState ShowcaseState { get; private set; }

        public FishData CurrentHookedFish { get; set; }

        private void Awake()
        {
            if (bobber != null)
            {
                BobberRb = bobber.GetComponent<Rigidbody2D>();
                DefaultGravity = BobberRb.gravityScale; 
            } 

            StateMachine = new FishingStateMachine();
            ReadyState = new ReadyState(this, StateMachine, "IsReady");
            CastingState = new CastingState(this, StateMachine, "IsCasting");
            SettledState = new SettledState(this, StateMachine, "IsSettled");
            BitingState = new BitingState(this, StateMachine, "IsBiting");
            RetrievingState = new RetrievingState(this, StateMachine, "IsRetrieving");
            MiniGameState = new MiniGameState(this, StateMachine, "IsMiniGame");
            FinalStruggleState = new FinalStruggleState(this, StateMachine, "IsMiniGame");
            FailedState = new FailedState(this, StateMachine, "IsFailed");
            CaughtState = new CaughtState(this, StateMachine, "IsCaught");
            ShowcaseState = new ShowcaseState(this, StateMachine, "IsShowcase");
        }

        private void Start()
        {
            if (fishingLine != null) fishingLine.Initialize(this); 
            StateMachine.Initialize(ReadyState);
            
            ResetPattern();
        }

        private void Update()
        {
            StateMachine.CurrentState?.Update();
        }

        private void FixedUpdate()
        {
            StateMachine.CurrentState?.FixedUpdate();
        }

        public void UpdateZoneMapping(float minX, float maxX, float centerX)
        {
            patternMinX = minX;
            patternMaxX = maxX;
            currentZoneCenterX = centerX;
        }

        public FishingZone FindNearestFishingZone()
        {
            FishingZone[] zones = FindObjectsByType<FishingZone>(FindObjectsSortMode.None);
            if (zones == null || zones.Length == 0) return null;
            
            FishingZone closest = null;
            float minDist = float.MaxValue;
            foreach (var zone in zones)
            {
                float dist = Vector2.Distance(transform.position, zone.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = zone;
                }
            }
            return closest;
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
            
            BobberRb.gravityScale = DefaultGravity; 
            BobberRb.linearDamping = 0f;
        }

        // 찌 비행 마찰로 유실되는 에너지를 사전에 예측 연산하여 초기 속도를 보정합니다.
        public void ApplyCastPhysics(float power)
        {
            if (BobberRb == null) return;
            BobberRb.bodyType = RigidbodyType2D.Dynamic;
            
            // 공기 저항 수치 적용
            BobberRb.linearDamping = castAirDamping; 

            if (ActiveFishingZone == null)
            {
                Vector2 fallbackVelocity = castDirection.normalized * (castSpeed * Mathf.Lerp(0.5f, 1.0f, power));
                BobberRb.linearVelocity = fallbackVelocity;
                return;
            }

            // 최대 파워(1.0) 일 때 마이너스 영역(-maxMoveRange)으로 투척 매핑 반전
            float targetOffset = Mathf.Lerp(ActiveFishingZone.maxMoveRange, -ActiveFishingZone.maxMoveRange, power);
            float targetX = ActiveFishingZone.transform.position.x + targetOffset;
            float targetY = ActiveFishingZone.transform.position.y;

            Vector3 startPos = GetTargetStartPosition();
            
            float g = Mathf.Abs(Physics2D.gravity.y) * BobberRb.gravityScale;
            float dx = targetX - startPos.x;
            float absDx = Mathf.Abs(dx);
            float dy = startPos.y - targetY; 

            // 좌우 방향성 수평각 판정
            Vector2 launchDir = castDirection.normalized;
            if (dx < 0) launchDir.x = -Mathf.Abs(launchDir.x);
            else launchDir.x = Mathf.Abs(launchDir.x);

            float angleRad = Mathf.Atan2(castDirection.y, Mathf.Abs(castDirection.x));
            float cosTheta = Mathf.Cos(angleRad);
            float tanTheta = Mathf.Sin(angleRad) / cosTheta;

            float denominator = 2f * cosTheta * cosTheta * (dy + absDx * tanTheta);
            if (denominator > 0.01f && absDx > 0.1f)
            {
                // 1. 공기 마찰이 없을 때의 순수 이상 속도 구하기
                float idealSpeed = absDx * Mathf.Sqrt(g / denominator);
                
                // 2. 가상 비행 시간 예측 수식 구현 ( t = (v0y + sqrt(v0y^2 + 2g*dy)) / g )
                float v0y = idealSpeed * Mathf.Sin(angleRad);
                float verticalTerm = (v0y * v0y) + (2f * g * dy);
                float estimatedFlightTime = 1.0f;
                if (verticalTerm >= 0f)
                {
                    estimatedFlightTime = (v0y + Mathf.Sqrt(verticalTerm)) / g;
                }

                // 3. 지수 속도 손실 보정 멀티플라이어 계산 ( Factor = (d * t) / (1 - e^(-d * t)) )
                float dragMultiplier = 1.0f;
                if (castAirDamping > 0.01f)
                {
                    float dt = castAirDamping * estimatedFlightTime;
                    dragMultiplier = dt / (1.0f - Mathf.Exp(-dt));
                }

                // 4. 감쇄가 메워진 최종 속도 조절
                float compensatedSpeed = idealSpeed * dragMultiplier;
                compensatedSpeed = Mathf.Clamp(compensatedSpeed, 2f, 120f); 
                
                Vector2 launchVelocity = launchDir * compensatedSpeed;
                BobberRb.linearVelocity = launchVelocity;
            }
            else
            {
                Vector2 launchVelocity = launchDir * (castSpeed * Mathf.Lerp(0.5f, 1.0f, power));
                BobberRb.linearVelocity = launchVelocity;
            }

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
        
        // 실시간 유효 기력 감소 속도 계산 (기본 스펙 + 장비 가산치)
        public float EffectiveDamageRate
        {
            get
            {
                if (playerEquipment != null && playerEquipment.EquippedRod != null)
                    return fishDamageRate + playerEquipment.EquippedRod.damageRateBonus;
                return fishDamageRate;
            }
        }

        // 실시간 유효 오차 허용 폭 계산 (기본 스펙 + 장비 가산치)
        public float EffectiveSweetSpotTolerance
        {
            get
            {
                if (playerEquipment != null && playerEquipment.EquippedRod != null)
                    return sweetSpotTolerance + playerEquipment.EquippedRod.sweetSpotBonus;
                return sweetSpotTolerance;
            }
        }
        
        // 현재 장착한 낚싯대의 파워 (장착 해제 시 기본 baseline인 10f 반환)
        public float EffectiveRodPower
        {
            get
            {
                if (playerEquipment != null && playerEquipment.EquippedRod != null)
                    return playerEquipment.EquippedRod.rodPower;
                return 10f; 
            }
        }

        // 현재 장착한 낚싯대의 민첩 상쇄 스펙 (장착 해제 시 기본 baseline인 5f 반환)
        public float EffectiveRodAgility
        {
            get
            {
                if (playerEquipment != null && playerEquipment.EquippedRod != null)
                    return playerEquipment.EquippedRod.rodAgility;
                return 5f; 
            }
        }

        public FishingZone SearchFishingZone()
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(bobber.position, detectionRadius, fishingZoneLayer);
            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<FishingZone>(out var zone))
                {
                    return zone;
                }
            }
            return null;
        }

        public void ResetPattern()
        {
            patternDrawer.enabled = false;
            patternDrawer.ClearAllDrawnLines();
            patternGenerator.ClearCurrentPattern();
            patternEvaluator.ResetEvaluation();
        }

        private void OnDestroy()
        {
            lineState.Dispose();
            FishUiRatio.Dispose();
            PlayerReelRatio.Dispose();
            LineStress.Dispose();
            FishHpRatio.Dispose();
            IsMiniGameActive.Dispose();
            CastPower.Dispose();
            IsCharging.Dispose();
            IsStruggleActive.Dispose();
            StruggleInkRatio.Dispose();
            ShowcaseFish.Dispose();
        }
        
        public void OnAnimationEvent_ThrowBobber()
        {
            if (StateMachine.CurrentState == CastingState)
            {
                CastingState.ExecuteCast();
            }
        }
        
        public void OnAnimationEvent_FailFinished()
        {
            if (StateMachine.CurrentState == FailedState)
            {
                StateMachine.ChangeState(ReadyState);
            }
        }
        
        public void OnAnimationEvent_PullBobber()
        {
            if (StateMachine.CurrentState == RetrievingState)
            {
                RetrievingState.ExecutePull();
            }
            else if (StateMachine.CurrentState == CaughtState)
            {
                CaughtState.ExecutePull();
            }
        }
        
        public void OnAnimationEvent_ShowcaseFish()
        {
            if (StateMachine.CurrentState == ShowcaseState)
            {
                ShowcaseState.RevealFish();
            }
        }
    }
}