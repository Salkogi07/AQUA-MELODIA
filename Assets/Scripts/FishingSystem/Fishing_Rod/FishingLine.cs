using UnityEngine;
using R3;
using FishingSystem.FishState;

namespace FishingSystem.Fishing_Rod
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLine : MonoBehaviour
    {
        [Header("낚시줄 두께 설정")]
        [Range(0.01f, 0.3f)] 
        public float lineWidth = 0.08f;

        [Header("목줄 (찌 ~ 바늘) 설정")]
        [Range(0.01f, 0.2f)]
        [Tooltip("목줄의 두께 (보통 메인 줄보다 가늘게 설정합니다)")]
        public float leaderLineWidth = 0.04f;
        [Tooltip("목줄의 투명도 (0: 투명, 1: 불투명)")]
        [Range(0f, 1f)]
        public float leaderLineAlpha = 0.6f;

        [Header("기본 로프 물리 설정 (대기 상태)")]
        [Range(5, 50)] 
        public int curveSegments = 15;       
        [Tooltip("대기 상태에서 축 늘어지는 정도")]
        public float sagAmount = 0.8f;       
        public float gravity = -12f;         
        [Range(0.8f, 0.99f)]
        public float damping = 0.9f;        
        [Range(1, 10)]
        public int stiffness = 4;            

        [Header("힘싸움(Taut) 상태 설정")]
        [Range(0f, 1f)]
        [Tooltip("1에 가까울수록 힘싸움할 때 줄이 완벽한 직선이 됩니다.")]
        public float tautTension = 0.93f; 
        
        [Tooltip("팽팽할 때 줄이 파르르 떨리는 진동 강도")]
        public float vibrationIntensity = 0.03f;
        [Tooltip("진동 속도 (높을수록 빠르게 떪)")]
        public float vibrationSpeed = 70f;

        private LineRenderer lineRenderer;          // 메인 낚시줄 (낚싯대 끝 ~ 찌)
        private LineRenderer bobberLineRenderer;    // 끊어짐 연출용 줄
        private LineRenderer leaderLineRenderer;    // 목줄 (찌 ~ 낚싯바늘)
        
        private FishingRod _ownerRod;
        private System.IDisposable stateSubscription;
        private FishingLineState currentState = FishingLineState.Slack;
        
        // 베를레 물리 배열
        private Vector3[] linePositions;
        private Vector3[] lineOldPositions;
        
        // 끊어짐 연출용 변수
        private Vector3 snapPoint;
        private float snapProgress = 0f;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            
            // 1. 끊어짐 연출용 두 번째 LineRenderer 생성
            GameObject childObj = new GameObject("BobberLineRenderer");
            childObj.transform.SetParent(this.transform);
            bobberLineRenderer = childObj.AddComponent<LineRenderer>();
            CopyLineRendererSettings(lineRenderer, bobberLineRenderer);
            bobberLineRenderer.positionCount = 0;

            // 2. [추가] 찌와 바늘을 잇는 목줄용 세 번째 LineRenderer 생성
            GameObject leaderObj = new GameObject("LeaderLineRenderer");
            leaderObj.transform.SetParent(this.transform);
            leaderLineRenderer = leaderObj.AddComponent<LineRenderer>();
            CopyLineRendererSettings(lineRenderer, leaderLineRenderer);
            
            // 목줄은 조금 더 투명한 색상으로 세팅
            Color originalColor = lineRenderer.startColor;
            originalColor.a = leaderLineAlpha;
            leaderLineRenderer.startColor = originalColor;
            leaderLineRenderer.endColor = originalColor;
            leaderLineRenderer.positionCount = 0;

            linePositions = new Vector3[curveSegments];
            lineOldPositions = new Vector3[curveSegments];
        }
        
        public void Initialize(FishingRod rod)
        {
            _ownerRod = rod;
            stateSubscription?.Dispose();
            
            ResetLinePositions();

            stateSubscription = _ownerRod.LineState.Subscribe(state => 
            {
                currentState = state;
                if (currentState == FishingLineState.Snapped)
                {
                    snapPoint = Vector3.Lerp(_ownerRod.rodTip.position, _ownerRod.bobber.position, 0.5f);
                    snapProgress = 0f;
                }
                else
                {
                    if (bobberLineRenderer != null) bobberLineRenderer.positionCount = 0;
                }
            });
        }

        private void ResetLinePositions()
        {
            if (_ownerRod == null) return;
            Vector3 start = _ownerRod.rodTip.position;
            Vector3 end = _ownerRod.bobber.position;
            
            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                linePositions[i] = pos;
                lineOldPositions[i] = pos;
            }
        }

        void LateUpdate()
        {
            if (_ownerRod == null || _ownerRod.rodTip == null || _ownerRod.bobber == null || lineRenderer == null) return;

            // 선 두께 실시간 업데이트
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            if (bobberLineRenderer != null)
            {
                bobberLineRenderer.startWidth = lineWidth;
                bobberLineRenderer.endWidth = lineWidth;
            }
            if (leaderLineRenderer != null)
            {
                leaderLineRenderer.startWidth = leaderLineWidth;
                leaderLineRenderer.endWidth = leaderLineWidth;
            }

            switch (currentState)
            {
                case FishingLineState.Taut:
                case FishingLineState.Slack:
                    SimulateRopePhysics();
                    DrawLeaderLine(); // 목줄 그리기
                    break;
                case FishingLineState.Snapped:
                    DrawSnappedLine();
                    HideLeaderLine(); // 줄이 끊어지면 목줄 숨김
                    break;
                case FishingLineState.None:
                    lineRenderer.positionCount = 0;
                    if (bobberLineRenderer != null) bobberLineRenderer.positionCount = 0;
                    HideLeaderLine(); // 자랑하기 등에서는 목줄도 함께 숨김
                    break;
            }
        }

        private void SimulateRopePhysics()
        {
            lineRenderer.positionCount = curveSegments;
            float dt = Time.deltaTime;

            float currentDist = Vector3.Distance(_ownerRod.rodTip.position, _ownerRod.bobber.position);
            float targetRopeLength = currentDist + (currentState == FishingLineState.Slack ? sagAmount : 0f);
            float targetSegmentLength = targetRopeLength / (curveSegments - 1);

            for (int i = 0; i < curveSegments; i++)
            {
                if (i == 0 || i == curveSegments - 1) continue;

                Vector3 velocity = (linePositions[i] - lineOldPositions[i]) * damping;
                lineOldPositions[i] = linePositions[i];
                linePositions[i] += velocity;
                
                float gravityMultiplier = (currentState == FishingLineState.Slack) ? 1.0f : 0.1f;
                linePositions[i].y += gravity * gravityMultiplier * dt * dt;
            }

            for (int iter = 0; iter < stiffness; iter++)
            {
                linePositions[0] = _ownerRod.rodTip.position;
                linePositions[curveSegments - 1] = _ownerRod.bobber.position;

                for (int i = 0; i < curveSegments - 1; i++)
                {
                    Vector3 node1 = linePositions[i];
                    Vector3 node2 = linePositions[i + 1];

                    float dist = Vector3.Distance(node1, node2);
                    float error = dist - targetSegmentLength;

                    if (dist > 0.001f)
                    {
                        Vector3 dir = (node1 - node2).normalized;
                        Vector3 change = dir * error * 0.5f;

                        if (i != 0) linePositions[i] -= change;
                        if (i + 1 != curveSegments - 1) linePositions[i + 1] += change;
                    }
                }
            }

            if (currentState == FishingLineState.Taut)
            {
                Vector3 lineDir = (_ownerRod.bobber.position - _ownerRod.rodTip.position).normalized;
                Vector3 perpDir = new Vector3(-lineDir.y, lineDir.x, 0f);
                float stress = _ownerRod.LineStress.Value;

                for (int i = 0; i < curveSegments; i++)
                {
                    float t = i / (float)(curveSegments - 1);
                    Vector3 straightPos = Vector3.Lerp(_ownerRod.rodTip.position, _ownerRod.bobber.position, t);

                    linePositions[i] = Vector3.Lerp(linePositions[i], straightPos, tautTension);

                    if (i > 0 && i < curveSegments - 1 && stress > 0.1f)
                    {
                        float wave = Mathf.Sin(Time.time * vibrationSpeed + i * 2f);
                        linePositions[i] += perpDir * wave * vibrationIntensity * stress;
                    }
                }
            }

            lineRenderer.SetPositions(linePositions);
        }

        // [추가] 찌와 바늘을 잇는 목줄 그리기 기능
        private void DrawLeaderLine()
        {
            if (_ownerRod.fishHookPoint == null || leaderLineRenderer == null) return;

            leaderLineRenderer.positionCount = 2;
            // 찌의 중심(혹은 찌 오브젝트의 위치)에서 아래 바늘 포인트까지 일직선으로 연결
            leaderLineRenderer.SetPosition(0, _ownerRod.bobber.position);
            leaderLineRenderer.SetPosition(1, _ownerRod.fishHookPoint.position);
        }

        private void HideLeaderLine()
        {
            if (leaderLineRenderer != null)
            {
                leaderLineRenderer.positionCount = 0;
            }
        }

        void DrawSnappedLine()
        {
            snapProgress += Time.deltaTime * 4f; 

            if (snapProgress >= 1f)
            {
                lineRenderer.positionCount = 0;
                bobberLineRenderer.positionCount = 0;
                ResetLinePositions(); 
                return;
            }

            lineRenderer.positionCount = curveSegments;
            bobberLineRenderer.positionCount = curveSegments;
            
            Vector3 rodStart = _ownerRod.rodTip.position;
            Vector3 currentRodBrokenEnd = Vector3.Lerp(snapPoint, rodStart, snapProgress);
            
            Vector3 rodControlPoint = Vector3.Lerp(rodStart, currentRodBrokenEnd, 0.5f);
            rodControlPoint.y -= Mathf.Sin(snapProgress * Mathf.PI * 3f) * 0.8f; 

            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                Vector3 point = CalculateQuadraticBezierPoint(t, rodStart, rodControlPoint, currentRodBrokenEnd);
                lineRenderer.SetPosition(i, point);
            }
            
            Vector3 bobberStart = _ownerRod.bobber.position;
            Vector3 currentBobberBrokenEnd = Vector3.Lerp(snapPoint, bobberStart, snapProgress);

            Vector3 bobberControlPoint = Vector3.Lerp(bobberStart, currentBobberBrokenEnd, 0.5f);
            bobberControlPoint.y += Mathf.Sin(snapProgress * Mathf.PI * 3f) * 0.5f; 

            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                Vector3 point = CalculateQuadraticBezierPoint(t, bobberStart, bobberControlPoint, currentBobberBrokenEnd);
                bobberLineRenderer.SetPosition(i, point);
            }
        }

        Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0; 
            p += 2 * u * t * p1; 
            p += tt * p2;        
            return p;
        }

        // 공통 속성을 하위 LineRenderer들에 복사해 주는 헬퍼 함수
        private void CopyLineRendererSettings(LineRenderer source, LineRenderer target)
        {
            target.sharedMaterial = source.sharedMaterial;
            target.colorGradient = source.colorGradient;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            target.numCapVertices = source.numCapVertices;
            target.numCornerVertices = source.numCornerVertices;
            target.textureMode = source.textureMode;
        }

        void OnDestroy()
        {
            stateSubscription?.Dispose();
        }
    }
}