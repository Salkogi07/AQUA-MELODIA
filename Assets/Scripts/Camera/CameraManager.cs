using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("카메라 이동 시 부드러운 정도를 조절하는 커브")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalPosition;
    private float originalOrthoSize;
    private Coroutine cameraCoroutine;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 초기 카메라 상태 저장
        originalPosition = mainCamera.transform.position;
        originalOrthoSize = mainCamera.orthographicSize;
    }

    /// <summary>
    /// 지정된 위치와 줌 크기로 카메라를 이동시킵니다.
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    /// <param name="targetOrthoSize">목표 줌 크기 (작을수록 확대)</param>
    /// <param name="duration">이동에 걸리는 시간</param>
    public void MoveCameraTo(Vector2 targetPosition, float targetOrthoSize, float duration)
    {
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(CameraTransitionRoutine(targetPosition, targetOrthoSize, duration));
    }

    /// <summary>
    /// 카메라를 원래 위치와 배율로 되돌립니다.
    /// </summary>
    public void ResetCamera(float duration)
    {
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(CameraTransitionRoutine(originalPosition, originalOrthoSize, duration));
    }

    private IEnumerator CameraTransitionRoutine(Vector2 targetPos, float targetSize, float duration)
    {
        Vector3 startPos = mainCamera.transform.position;
        // 2D 카메라의 Z축(일반적으로 -10)은 유지해야 화면이 깨지지 않습니다.
        Vector3 finalPos = new Vector3(targetPos.x, targetPos.y, startPos.z);
        
        float startSize = mainCamera.orthographicSize;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;
            float curveT = transitionCurve.Evaluate(t); // 커브를 적용해 더 자연스럽게 연출

            mainCamera.transform.position = Vector3.Lerp(startPos, finalPos, curveT);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, curveT);

            yield return null;
        }

        // 최종 값 정확히 맞추기
        mainCamera.transform.position = finalPos;
        mainCamera.orthographicSize = targetSize;
    }
}