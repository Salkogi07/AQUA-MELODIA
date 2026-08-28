using UnityEngine;
using System;
using R3; // R3 기능 탑재

namespace WorldTime
{
    public class WorldTime : MonoBehaviour
    {
        public static WorldTime Instance { get; private set; }

        [Header("시간 설정")]
        [Tooltip("게임 내 하루가 실제 현실에서 몇 분 동안 지속될지 설정합니다.")]
        [SerializeField] private float _dayLengthInMinutes = 20f;

        // R3 ReactiveProperty로 실시간 시간 변경 관리 (초기값: 07시)
        private readonly ReactiveProperty<TimeSpan> _currentTime = new(TimeSpan.FromHours(7));
        public ReadOnlyReactiveProperty<TimeSpan> CurrentTime => _currentTime;

        private float _timer;

        private void Awake()
        {
            // 다른 씬으로 넘어가도 파괴되지 않고 시간이 계속 유지되는 싱글톤 처리
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            UpdateCustomTime();
        }

        private void UpdateCustomTime()
        {
            _timer += Time.deltaTime;
            
            float realSecondsForGameMinute = (_dayLengthInMinutes * 60f) / WorldTimeConstants.MinutesInDay;

            if (_timer >= realSecondsForGameMinute)
            {
                TimeSpan nextTime = _currentTime.Value.Add(TimeSpan.FromMinutes(1));
                
                // 24시간 초과 시 자정(0시) 기준으로 보정
                if (nextTime.TotalDays >= 1)
                {
                    nextTime = nextTime.Subtract(TimeSpan.FromDays(1));
                }
                
                _currentTime.Value = nextTime;
                _timer = 0;
            }
        }
    }
}