using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WorldTime
{
    [RequireComponent(typeof(Light2D))]
    public class WorldLight : MonoBehaviour
    {
        private Light2D _light;

        [SerializeField] private WorldTime _worldTime;
        
        [Header("시각 효과")]
        [SerializeField] private Gradient _gradient;       // 시간에 따른 색상 (0: 자정, 0.5: 정오)
        [SerializeField] private AnimationCurve _intensityCurve; // 시간에 따른 밝기

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _worldTime.WorldTimeChanged += OnWorldTimeChanged;
        }

        private void OnDestroy()
        {
            if (_worldTime != null)
                _worldTime.WorldTimeChanged -= OnWorldTimeChanged;
        }

        private void OnWorldTimeChanged(object sender, TimeSpan newTime)
        {
            // 하루 중 진행률 (0.0 ~ 1.0)
            float timePercent = PercentOfDay(newTime);
            
            // 최종 색상 및 밝기 적용 (계절 보정 없이 직접 적용)
            _light.color = _gradient.Evaluate(timePercent);
            _light.intensity = _intensityCurve.Evaluate(timePercent);
        }

        private float PercentOfDay(TimeSpan timeSpan)
        {
            return (float)timeSpan.TotalMinutes / WorldTimeConstants.MinutesInDay;
        }
    }
}