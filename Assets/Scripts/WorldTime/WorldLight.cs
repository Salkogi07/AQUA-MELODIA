using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using R3; // R3 기능 탑재

namespace WorldTime
{
    [RequireComponent(typeof(Light2D))]
    public class WorldLight : MonoBehaviour
    {
        private Light2D _light;

        [SerializeField] private WorldTime _worldTime;
        
        [Header("시각 효과")]
        [SerializeField] private Gradient _gradient;       
        [SerializeField] private AnimationCurve _intensityCurve; 

        private void Start()
        {
            _light = GetComponent<Light2D>();

            // 직렬화 할당이 비어 있을 경우 싱글톤 인스턴스 자동 탐색
            if (_worldTime == null)
            {
                _worldTime = WorldTime.Instance;
            }

            if (_worldTime != null)
            {
                // R3 스트림 구독 처리 및 생명주기에 맞춘 자동 해제 등록
                _worldTime.CurrentTime
                    .Subscribe(newTime => ApplyLightSettings(newTime))
                    .AddTo(this);
            }
        }

        private void ApplyLightSettings(TimeSpan time)
        {
            float timePercent = PercentOfDay(time);
            
            _light.color = _gradient.Evaluate(timePercent);
            _light.intensity = _intensityCurve.Evaluate(timePercent);
        }

        private float PercentOfDay(TimeSpan timeSpan)
        {
            return (float)timeSpan.TotalMinutes / WorldTimeConstants.MinutesInDay;
        }
    }
}