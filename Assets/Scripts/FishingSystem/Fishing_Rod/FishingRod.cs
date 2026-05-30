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
        [SerializeField] private Transform rodTip;           // 낚싯대 끝 위치
        [SerializeField] private Transform bobber;           // 낚시찌 위치
        [SerializeField] private Transform castStartPosition; // 던지기 시작할 가상의 위치

        [Header("캐스팅(속도) 설정")]
        [SerializeField] private Vector2 castDirection = new Vector2(1.2f, 1f); 
        [Range(0f, 100f)] 
        [SerializeField] private float castSpeed = 25f;                       

        private Rigidbody2D bobberRb;
        private CancellationTokenSource castCts;
        
        // 찌가 날아갔는지 여부를 체크하는 플래그
        private bool isCasted = false;

        // R3: 상태 관리
        private readonly ReactiveProperty<FishingLineState> lineState = new(FishingLineState.Slack);
        public ReadOnlyReactiveProperty<FishingLineState> LineState => lineState;

        public Transform RodTip => rodTip;
        public Transform Bobber => bobber;

        void Start()
        {
            if (bobber != null)
            {
                bobberRb = bobber.GetComponent<Rigidbody2D>();
            }

            if (fishingLine != null)
            {
                fishingLine.Initialize(this);
            }

            // [신규] 시작 시 찌를 대기 상태(고정)로 설정
            ResetBobberToReady();
        }

        void Update()
        {
            // [신규] 던지기 전 상태라면, 캐릭터나 낚싯대가 움직여도 찌가 끝에 딱 붙어 부드럽게 쫓아오도록 고정
            if (!isCasted && bobber != null)
            {
                bobber.position = GetTargetStartPosition();
            }
        }

        /// <summary>
        /// [신규] 찌를 낚싯대 끝에 물리 연산 없이 단단히 고정하는 메서드
        /// (초기 시작 시, 또는 물고기를 잡고 난 후 다시 세팅할 때 호출 가능)
        /// </summary>
        public void ResetBobberToReady()
        {
            isCasted = false;
            lineState.Value = FishingLineState.Slack;

            if (bobber != null)
            {
                bobber.position = GetTargetStartPosition();
            }

            if (bobberRb != null)
            {
                // Kinematic으로 변경하여 중력이나 다른 마찰력의 영향을 받지 않고 고정시킴
                bobberRb.bodyType = RigidbodyType2D.Kinematic;
                bobberRb.linearVelocity = Vector2.zero;
                bobberRb.angularVelocity = 0f;
            }
        }

        /// <summary>
        /// 찌를 조절된 속도로 발사 (발사 순간 고정이 풀립니다)
        /// </summary>
        public async UniTaskVoid CastBobberAsync()
        {
            if (bobber == null || bobberRb == null) return;

            castCts?.Cancel();
            castCts?.Dispose();
            castCts = new CancellationTokenSource();

            // 1. 발사 플래그 세팅 및 위치 최종 동기화
            isCasted = true;
            bobber.position = GetTargetStartPosition();

            // 2. 물리 상태를 Dynamic으로 변경하여 중력과 속도가 적용되도록 전환
            bobberRb.bodyType = RigidbodyType2D.Dynamic;

            // 3. 정규화된 방향 벡터에 속도를 꽂아 넣어 즉시 날아가게 처리
            Vector2 launchVelocity = castDirection.normalized * castSpeed;
            bobberRb.linearVelocity = launchVelocity;
            bobberRb.angularVelocity = 0f;

            lineState.Value = FishingLineState.Slack;

            Debug.Log($"<color=lime>🚀 캐스팅 발사! 속도: {castSpeed}</color>");

            try
            {
                // 찌가 날아가다가 어딘가(물 등)에 안착해서 멈출 때까지 대기
                await UniTask.WaitUntil(() => bobberRb.linearVelocity.magnitude < 0.2f, cancellationToken: castCts.Token);
                Debug.Log("<color=yellow>🌊 찌 안착 완료, 입질 대기 중...</color>");
            }
            catch (System.OperationCanceledException)
            {
                // 예외 처리
            }
        }

        public void OnFishBite()
        {
            lineState.Value = FishingLineState.Taut;
            Debug.Log("<color=red>🎯 줄 팽팽함! 물고기가 물었습니다.</color>");
        }

        // 공통 위치 반환 헬퍼 메서드
        private Vector3 GetTargetStartPosition()
        {
            return castStartPosition != null ? castStartPosition.position : rodTip.position;
        }

        void OnDestroy()
        {
            castCts?.Cancel();
            castCts?.Dispose();
            lineState.Dispose();
        }
    }
}