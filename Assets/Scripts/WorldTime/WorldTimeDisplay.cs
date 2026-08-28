using System;
using UnityEngine;
using TMPro;
using R3; // R3 기능 탑재

namespace WorldTime
{
    [RequireComponent(typeof(TMP_Text))]
    public class WorldTimeDisplay : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        private TMP_Text _text;

        private void Start()
        {
            _text = GetComponent<TMP_Text>();

            // 직렬화 할당이 비어 있을 경우 싱글톤 인스턴스 자동 탐색
            if (_worldTime == null)
            {
                _worldTime = WorldTime.Instance;
            }

            if (_worldTime != null)
            {
                // R3 스트림 구독 처리 및 생명주기에 맞춘 자동 해제 등록
                _worldTime.CurrentTime
                    .Subscribe(newTime => UpdateText(newTime))
                    .AddTo(this);
            }
        }

        private void UpdateText(TimeSpan time)
        {
            _text.SetText(time.ToString(@"hh\:mm"));
        }
    }
}