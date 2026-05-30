using UnityEngine;
using R3;

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
        private FishingRod ownerRod;
        private System.IDisposable stateSubscription;
        private FishingLineState currentState = FishingLineState.Slack;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        public void Initialize(FishingRod rod)
        {
            ownerRod = rod;
            stateSubscription?.Dispose();
            
            stateSubscription = ownerRod.LineState
                .Subscribe(state => currentState = state);
        }

        void LateUpdate()
        {
            if (ownerRod == null || ownerRod.RodTip == null || ownerRod.Bobber == null || lineRenderer == null) return;

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            if (currentState == FishingLineState.Taut)
            {
                DrawStraightLine();
            }
            else
            {
                DrawCurvedLine();
            }
        }

        void DrawStraightLine()
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, ownerRod.RodTip.position);
            lineRenderer.SetPosition(1, ownerRod.Bobber.position);
        }

        void DrawCurvedLine()
        {
            lineRenderer.positionCount = curveSegments;

            Vector3 start = ownerRod.RodTip.position;
            Vector3 end = ownerRod.Bobber.position;

            Vector3 controlPoint = Vector3.Lerp(start, end, 0.5f);
            controlPoint.y -= sagAmount;

            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                Vector3 point = CalculateQuadraticBezierPoint(t, start, controlPoint, end);
                lineRenderer.SetPosition(i, point);
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