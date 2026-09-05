using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraTargetPoint : MonoBehaviour
{
    [Header("Camera Target Settings")]
    [Tooltip("이 위치로 이동할 때의 줌 배율 (1.0 = 기본, 2.0 = 2배 확대)")]
    public float targetZoomFactor = 2f;

    [Tooltip("기지모 색상")]
    public Color gizmoColor = Color.green;

    // 에디터 화면에서 Pixel Perfect 카메라 기준 실제 비춰질 영역을 박스로 표시
    private void OnDrawGizmos()
    {
        PixelPerfectCamera ppc = null;
        if (Camera.main != null)
        {
            ppc = Camera.main.GetComponent<PixelPerfectCamera>();
        }

        float width, height;

        if (ppc != null)
        {
            // Pixel Perfect Camera의 Reference Resolution 및 PPU 기반 실제 픽셀 범위 연산
            float zoom = targetZoomFactor > 0 ? targetZoomFactor : 1f;
            float targetPPU = ppc.assetsPPU * zoom;

            width = ppc.refResolutionX / targetPPU;
            height = ppc.refResolutionY / targetPPU;
        }
        else
        {
            // 예외 처리 (기본 16:9 비율)
            height = (5f / (targetZoomFactor > 0 ? targetZoomFactor : 1f)) * 2f;
            width = height * (16f / 9f);
        }

        Gizmos.color = gizmoColor;
        
        // Target 위치 십자선
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.down * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);

        // 연출 시 비춰질 정확한 카메라 영역 WireBox
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
    }

    /// <summary>
    /// 이 위치와 줌 배율로 카메라 연출을 실행하는 함수
    /// </summary>
    public void PlayCameraAction(float duration = 1.5f)
    {
        CameraManager.Instance.MoveCameraTo(transform.position, targetZoomFactor, duration);
    }
}