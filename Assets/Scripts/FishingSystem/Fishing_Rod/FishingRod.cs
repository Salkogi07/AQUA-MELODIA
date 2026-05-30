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
        }

        /// <summary>
        /// 찌를 던지는 로직 (이제 안착해도 줄이 자동으로 팽팽해지지 않습니다)
        /// </summary>
        public async UniTaskVoid CastBobberAsync()
        {
            if (bobber == null || bobberRb == null) return;

            castCts?.Cancel();
            castCts?.Dispose();
            castCts = new CancellationTokenSource();

            // 1. 초기 위치 및 속도 세팅
            Vector3 startPos = castStartPosition != null ? castStartPosition.position : rodTip.position;
            bobberRb.transform.position = startPos;

            Vector2 launchVelocity = castDirection.normalized * castSpeed;
            bobberRb.linearVelocity = launchVelocity;
            bobberRb.angularVelocity = 0f;

            // 던질 때는 당연히 느슨한 상태
            lineState.Value = FishingLineState.Slack;

            Debug.Log($"<color=lime>🚀 캐스팅 완료! (속도: {castSpeed})</color>");

            try
            {
                // 찌가 날아가다 물에 안착(속도가 줄어듦)할 때까지만 대기
                await UniTask.WaitUntil(() => bobberRb.linearVelocity.magnitude < 0.2f, cancellationToken: castCts.Token);
                
                // 이제 안착해도 계속 Slack(느슨한 곡선) 상태를 유지합니다.
                Debug.Log("<color=yellow>🌊 찌가 물에 안착했습니다. 물고기의 입질을 기다립니다...</color>");
            }
            catch (System.OperationCanceledException)
            {
                // 취소 예외 처리
            }
        }

        /// <summary>
        /// 물고기가 미끼를 물었을 때 외부 시스템에서 호출해주는 메서드
        /// </summary>
        public void OnFishBite()
        {
            // 물고기가 무는 순간 낚시줄을 팽팽하게 변경
            lineState.Value = FishingLineState.Taut;
            
            Debug.Log("<color=red>🎯 찌릿! 물고기가 미끼를 물었습니다! 줄이 팽팽해집니다.</color>");
        }

        void OnDestroy()
        {
            castCts?.Cancel();
            castCts?.Dispose();
            lineState.Dispose();
        }
    }
}