using UnityEngine;
using System;

namespace WorldTime
{
    public class WorldTime : MonoBehaviour
    {
        public event EventHandler<TimeSpan> WorldTimeChanged;
        
        [Header("시간 설정")]
        [Tooltip("게임 내 하루가 실제 현실에서 몇 분 동안 지속될지 설정합니다.")]
        [SerializeField] private float _dayLengthInMinutes = 20f;

        private TimeSpan _currentTime;
        private float _timer;

        private void Start()
        {
            // 00:00:00(자정)부터 시작합니다. 
            //_currentTime = TimeSpan.Zero;
            _currentTime = TimeSpan.FromHours(7);
        }

        private void Update()
        {
            UpdateCustomTime();
        }

        private void UpdateCustomTime()
        {
            _timer += Time.deltaTime;
            
            // 게임 내 1분을 처리하기 위한 실제 현실의 초 계산
            float realSecondsForGameMinute = (_dayLengthInMinutes * 60f) / WorldTimeConstants.MinutesInDay;

            if (_timer >= realSecondsForGameMinute)
            {
                _currentTime = _currentTime.Add(TimeSpan.FromMinutes(1));
                
                // 24시간이 지나면 다시 0시로 초기화
                if (_currentTime.TotalDays >= 1)
                {
                    _currentTime = _currentTime.Subtract(TimeSpan.FromDays(1));
                }
                
                _timer = 0;
                WorldTimeChanged?.Invoke(this, _currentTime);
            }
        }

        // 현재 시간을 외부에서 확인하고 싶을 때를 위한 속성
        public TimeSpan CurrentTime => _currentTime;
    }
}