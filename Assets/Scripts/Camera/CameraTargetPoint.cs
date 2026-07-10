using UnityEngine;

public class CameraTargetPoint : MonoBehaviour
{
    [Header("Camera Target Settings")]
    [Tooltip("이 위치로 이동할 때의 카메라 Size (작을수록 확대됨)")]
    public float targetZoomSize = 5f;
    [Tooltip("기지모 색상 (구분하기 쉽게 변경 가능)")]
    public Color gizmoColor = Color.green;

    // 에디터 화면에서 카메라가 비출 영역을 기지모로 그려줍니다.
    private void OnDrawGizmos()
    {
        // 2D 카메라의 화면 비율 (16:9 기준, 실제 카메라가 있다면 카메라 비율 사용)
        float aspect = 16f / 9f; 
        if (Camera.main != null)
        {
            aspect = Camera.main.aspect;
        }

        // 2D Orthographic 카메라의 세로 길이는 Size * 2, 가로는 세로 * 비율입니다.
        float height = targetZoomSize * 2f;
        float width = height * aspect;

        Gizmos.color = gizmoColor;
        // 위치에 십자선 그리기
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.down * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);
        
        // 카메라가 비출 실제 영역(사각형) 그리기
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
    }
    
    // 이 위치로 카메라를 연출하는 편의용 함수
    public void PlayCameraAction(float duration = 1.5f)
    {
        CameraManager.Instance.MoveCameraTo(transform.position, targetZoomSize, duration);
    }
}