using UnityEngine;
using UnityEngine.Profiling;

public class PerformanceMonitor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // 측정 갱신 주기
    [SerializeField] private bool showGUI = true;         // 시작 시 GUI 표시 여부

    private float accum = 0.0f;
    private int frames = 0;
    private float timeleft;

    // 측정 데이터 변수
    private float fps = 0.0f;
    private float frameTimeMs = 0.0f;
    private float usedHeapMemoryMB = 0.0f;
    private float allocatedMemoryMB = 0.0f;
    private float totalReservedMemoryMB = 0.0f;

    // 시스템 하드웨어 정보
    private string cpuInfo;
    private string gpuInfo;
    private int gpuMemorySizeMB;

    // OnGUI 렌더링 스타일
    private GUIStyle labelStyle;
    private Texture2D bgTexture;

    private void Start()
    {
        timeleft = updateInterval;

        // 하드웨어 기본 정보 캐싱
        cpuInfo = SystemInfo.processorType;
        gpuInfo = SystemInfo.graphicsDeviceName;
        gpuMemorySizeMB = SystemInfo.graphicsMemorySize;

        InitGUIStyle();
    }

    private void InitGUIStyle()
    {
        // OnGUI 가독성을 위한 반투명 검은색 배경 텍스처 생성
        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        bgTexture.Apply();

        labelStyle = new GUIStyle();
        labelStyle.fontSize = 15;
        labelStyle.normal.textColor = Color.white;
        labelStyle.padding = new RectOffset(10, 10, 10, 10);
        labelStyle.normal.background = bgTexture;
    }

    private void Update()
    {
        // F3 키 입력 시 OnGUI 화면 토글
        if (Input.GetKeyDown(KeyCode.F3))
        {
            showGUI = !showGUI;
        }

        timeleft -= Time.unscaledDeltaTime;
        accum += Time.unscaledDeltaTime;
        frames++;

        if (timeleft <= 0.0f)
        {
            // 1. FPS & Frame Time
            fps = frames / accum;
            frameTimeMs = (accum / frames) * 1000.0f;

            // 2. RAM & GC Memory
            usedHeapMemoryMB = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);
            allocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            totalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);

            // 리셋
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }

    private string GetFormattedStats()
    {
        return $"[PERFORMANCE MONITOR]\n" +
               $"FPS: {fps:F1} ({frameTimeMs:F1} ms)\n" +
               $"----------------------------------------\n" +
               $"CPU: {cpuInfo} ({SystemInfo.processorCount} Threads)\n" +
               $"GPU: {gpuInfo} (VRAM: {gpuMemorySizeMB} MB)\n" +
               $"----------------------------------------\n" +
               $"Mono Heap (GC): {usedHeapMemoryMB:F1} MB\n" +
               $"Allocated RAM: {allocatedMemoryMB:F1} MB\n" +
               $"Reserved RAM: {totalReservedMemoryMB:F1} MB";
    }

    private void OnGUI()
    {
        if (!showGUI) return;

        if (labelStyle == null || bgTexture == null)
        {
            InitGUIStyle();
        }

        // FPS 수치에 따라 텍스트 색상 변경 (60이상: 녹색, 30이상: 노란색, 이하: 빨간색)
        labelStyle.normal.textColor = fps >= 55f ? Color.green : (fps >= 30f ? Color.yellow : Color.red);

        // 화면 좌측 상단에 텍스트와 배경 박스를 자동으로 크기 맞춰 출력
        GUI.Box(new Rect(10, 10, 420, 170), GetFormattedStats(), labelStyle);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지용 텍스처 해제
        if (bgTexture != null)
        {
            Destroy(bgTexture);
        }
    }
}