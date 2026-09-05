using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    
    [Tooltip("카메라 이동 시 부드러운 정도를 조절하는 커브")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalPosition;
    private float baseOrthoSize;
    private Coroutine cameraCoroutine;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (mainCamera == null) mainCamera = Camera.main;
        if (pixelPerfectCamera == null && mainCamera != null)
        {
            pixelPerfectCamera = mainCamera.GetComponent<PixelPerfectCamera>();
        }

        // 초기 위치 및 기준 Orthographic Size 저장
        originalPosition = mainCamera.transform.position;
        if (mainCamera != null)
        {
            baseOrthoSize = mainCamera.orthographicSize;
        }
    }

    /// <summary>
    /// 지정된 위치와 줌 배율(zoomFactor)로 카메라를 부드럽게 이동시킵니다.
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    /// <param name="zoomFactor">줌 배율 (1.0 = 기본, 2.0 = 2배 확대)</param>
    /// <param name="duration">이동에 걸리는 시간</param>
    public void MoveCameraTo(Vector2 targetPosition, float zoomFactor, float duration)
    {
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(CameraTransitionRoutine(targetPosition, zoomFactor, duration));
    }

    /// <summary>
    /// 카메라를 원래 위치와 배율로 되돌립니다.
    /// </summary>
    public void ResetCamera(float duration)
    {
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(CameraTransitionRoutine(originalPosition, 1.0f, duration));
    }

    private IEnumerator CameraTransitionRoutine(Vector2 targetPos, float targetZoomFactor, float duration)
    {
        Vector3 startPos = mainCamera.transform.position;
        Vector3 finalPos = new Vector3(targetPos.x, targetPos.y, startPos.z);

        float startSize = mainCamera.orthographicSize;
        // targetZoomFactor가 2.0이면 목표 크기는 절반(1/2)으로 줄어들어 화면이 2배 확대됩니다.
        float finalSize = baseOrthoSize / Mathf.Max(0.001f, targetZoomFactor);

        // 1. 연출 도중 끊김(드드득거림) 방지를 위해 PixelPerfectCamera 잠시 끄기
        if (pixelPerfectCamera != null)
        {
            pixelPerfectCamera.enabled = false;
        }

        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;
            float curveT = transitionCurve.Evaluate(t);

            // 위치 및 크기(Orthographic Size) 부드러운 보간
            mainCamera.transform.position = Vector3.Lerp(startPos, finalPos, curveT);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, finalSize, curveT);

            yield return null;
        }

        // 2. 최종 위치/크기 맞추기
        mainCamera.transform.position = finalPos;
        mainCamera.orthographicSize = finalSize;

        // 3. 줌 연출이 끝나고 기본 배율(1.0)로 돌아왔을 때만 PixelPerfectCamera 재활성화
        if (pixelPerfectCamera != null && Mathf.Approximately(targetZoomFactor, 1.0f))
        {
            pixelPerfectCamera.enabled = true;
        }
    }
}