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

        [Header("느슨한 줄(곡선) 세부 설정")]
        [Range(5, 30)] 
        public int curveSegments = 15;    
        public float sagAmount = 0.5f;     

        private LineRenderer lineRenderer;
        private LineRenderer bobberLineRenderer;
        private FishingRod _ownerRod;
        private System.IDisposable stateSubscription;
        private FishingLineState currentState = FishingLineState.Slack;
        
        // 끊어짐 연출용 변수
        private Vector3 snapPoint;
        private float snapProgress = 0f;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            
            // 찌 쪽 줄을 그리기 위한 두 번째 LineRenderer 자동 생성 및 설정 복사
            GameObject childObj = new GameObject("BobberLineRenderer");
            childObj.transform.SetParent(this.transform);
            bobberLineRenderer = childObj.AddComponent<LineRenderer>();
            
            // 메인 선의 재질, 색상, 레이어 설정 등을 그대로 가져옴
            bobberLineRenderer.sharedMaterial = lineRenderer.sharedMaterial;
            bobberLineRenderer.colorGradient = lineRenderer.colorGradient;
            bobberLineRenderer.sortingLayerID = lineRenderer.sortingLayerID;
            bobberLineRenderer.sortingOrder = lineRenderer.sortingOrder;
            bobberLineRenderer.numCapVertices = lineRenderer.numCapVertices;
            bobberLineRenderer.numCornerVertices = lineRenderer.numCornerVertices;
            bobberLineRenderer.textureMode = lineRenderer.textureMode;
            bobberLineRenderer.positionCount = 0;
        }
        
        public void Initialize(FishingRod rod)
        {
            _ownerRod = rod;
            stateSubscription?.Dispose();
            
            stateSubscription = _ownerRod.LineState.Subscribe(state => 
            {
                currentState = state;
                if (currentState == FishingLineState.Snapped)
                {
                    // 끊어지는 순간의 정중앙 지점 기억
                    snapPoint = Vector3.Lerp(_ownerRod.rodTip.position, _ownerRod.bobber.position, 0.5f);
                    snapProgress = 0f;
                }
                else
                {
                    // 끊어짐 상태가 아니면 찌 쪽 선을 즉시 숨김
                    if (bobberLineRenderer != null) bobberLineRenderer.positionCount = 0;
                }
            });
        }

        void LateUpdate()
        {
            if (_ownerRod == null || _ownerRod.rodTip == null || _ownerRod.bobber == null || lineRenderer == null) return;

            // 선 두께 갱신
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            if (bobberLineRenderer != null)
            {
                bobberLineRenderer.startWidth = lineWidth;
                bobberLineRenderer.endWidth = lineWidth;
            }

            switch (currentState)
            {
                case FishingLineState.Taut:
                    DrawStraightLine();
                    break;
                case FishingLineState.Slack:
                    DrawCurvedLine();
                    break;
                case FishingLineState.Snapped:
                    DrawSnappedLine();
                    break;
            }
        }

        void DrawStraightLine()
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, _ownerRod.rodTip.position);
            lineRenderer.SetPosition(1, _ownerRod.bobber.position);
        }

        void DrawCurvedLine()
        {
            lineRenderer.positionCount = curveSegments;

            Vector3 start = _ownerRod.rodTip.position;
            Vector3 end = _ownerRod.bobber.position;

            Vector3 controlPoint = Vector3.Lerp(start, end, 0.5f);
            controlPoint.y -= sagAmount;

            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                Vector3 point = CalculateQuadraticBezierPoint(t, start, controlPoint, end);
                lineRenderer.SetPosition(i, point);
            }
        }
        
        // 끊어진 줄이 낚싯대 방향으로 휙! 말려들어가는 연출
        void DrawSnappedLine()
        {
            snapProgress += Time.deltaTime * 4f; // 0.25초 만에 사라짐 (스피디한 연출)

            if (snapProgress >= 1f)
            {
                // 말려들어가는 연출이 끝나면 두 선 모두 지움
                lineRenderer.positionCount = 0;
                bobberLineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.positionCount = curveSegments;
            bobberLineRenderer.positionCount = curveSegments;
            
            // 1. 낚싯대 쪽 선 (위로 말려 올라감)
            Vector3 rodStart = _ownerRod.rodTip.position;
            Vector3 currentRodBrokenEnd = Vector3.Lerp(snapPoint, rodStart, snapProgress);
            
            Vector3 rodControlPoint = Vector3.Lerp(rodStart, currentRodBrokenEnd, 0.5f);
            rodControlPoint.y -= Mathf.Sin(snapProgress * Mathf.PI * 3f) * 0.8f; // 요동침

            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                Vector3 point = CalculateQuadraticBezierPoint(t, rodStart, rodControlPoint, currentRodBrokenEnd);
                lineRenderer.SetPosition(i, point);
            }
            
            // 2. 찌 쪽 선 (아래로 말려 내려감)
            Vector3 bobberStart = _ownerRod.bobber.position;
            Vector3 currentBobberBrokenEnd = Vector3.Lerp(snapPoint, bobberStart, snapProgress);

            Vector3 bobberControlPoint = Vector3.Lerp(bobberStart, currentBobberBrokenEnd, 0.5f);
            // 찌 쪽 선은 낚싯대와 반대로 요동치게 연출
            bobberControlPoint.y += Mathf.Sin(snapProgress * Mathf.PI * 3f) * 0.5f; 

            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                // 찌에서 끊어진 끝부분까지 선을 그림
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

        void OnDestroy()
        {
            stateSubscription?.Dispose();
        }
    }
}