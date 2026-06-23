using UnityEngine;
using System;

namespace WorldTime
{
    public class WorldTime : MonoBehaviour
    {
        public event EventHandler<TimeSpan> WorldTimeChanged;

        [Header("Settings")]
        [SerializeField] private bool _useRealTime; // 현실 시간 사용 여부 (On/Off)
        [SerializeField] private float _dayLengthInSeconds = 120f; // 게임 시간용: 하루가 현실에서 몇 초인지

        private TimeSpan _currentTime;
        private float _timer;

        // 외부에서 On/Off를 조절할 수 있는 프로퍼티
        public bool UseRealTime
        {
            get => _useRealTime;
            set => _useRealTime = value;
        }

        private void Update()
        {
            if (_useRealTime)
            {
                UpdateRealTime();
            }
            else
            {
                UpdateCustomTime();
            }
        }

        private void UpdateRealTime()
        {
            // 현재 시스템의 시간을 가져와서 이벤트 발생
            TimeSpan now = DateTime.Now.TimeOfDay;
            
            // 1초마다 이벤트를 보내고 싶다면 아래와 같이 체크 가능 (성능 최적화)
            if (now.Seconds != _currentTime.Seconds)
            {
                _currentTime = now;
                WorldTimeChanged?.Invoke(this, _currentTime);
            }
        }

        private void UpdateCustomTime()
        {
            // 게임 내 시간을 흐르게 하는 로직 (기존 코루틴 방식보다 정교함)
            _timer += Time.deltaTime;
            float secondRatio = WorldTimeConstants.MinutesInDay * 60f / _dayLengthInSeconds;
            
            // 실제 흘러야 할 게임 내 시간(초) 계산
            double totalSecondsToAdd = Time.deltaTime * (86400f / _dayLengthInSeconds);
            _currentTime = _currentTime.Add(TimeSpan.FromSeconds(totalSecondsToAdd));

            // 날짜가 넘어갈 때를 위해 24시간 주기로 조절
            if (_currentTime.TotalDays >= 1)
            {
                _currentTime = _currentTime.Subtract(TimeSpan.FromDays(1));
            }

            WorldTimeChanged?.Invoke(this, _currentTime);
        }
    }
}