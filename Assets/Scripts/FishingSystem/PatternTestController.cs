using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using FishingSystem.Pattern;

public class PatternTestController : MonoBehaviour
{
    [SerializeField] private PatternGenerator patternGenerator;
    [SerializeField] private EscapePatternData testPattern;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // 패턴 그리기를 비동기로 실행 (중복 실행 방지 로직 내장됨)
            patternGenerator.GeneratePatternAsync(testPattern).Forget();
        }
    }
}