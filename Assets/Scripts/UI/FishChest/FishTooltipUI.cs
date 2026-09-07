using UnityEngine;
using UnityEngine.UI;
using FishingSystem.Fish;

namespace FishingSystem.UI
{
    public class FishTooltipUI : MonoBehaviour
    {
        [Header("UI 구성 요소")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text gradeText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text lengthText;

        [Header("위치 오프셋 (슬롯 기준 로컬 단위)")]
        [Tooltip("호버된 슬롯의 중심점을 기준으로 툴팁이 배치될 오프셋입니다.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 120f);

        private RectTransform _rectTransform;
        private RectTransform _targetSlotRect;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // 타겟 슬롯이 있고 활성화된 상태라면 매 프레임 위치 동기화 (UI 이동 대응)
            if (tooltipPanel != null && tooltipPanel.activeSelf && _targetSlotRect != null)
            {
                UpdatePositionToTarget();
            }
        }

        public void Show(FishData fish, RectTransform slotRect)
        {
            if (fish == null || tooltipPanel == null || slotRect == null) return;

            _targetSlotRect = slotRect;

            if (gradeText != null) gradeText.text = $"등급: {fish.Data.grade}";
            if (nameText != null) nameText.text = $"이름: {fish.Data.fishName}";
            if (lengthText != null) lengthText.text = $"길이: {fish.Length:F1}cm";

            tooltipPanel.SetActive(true);
            UpdatePositionToTarget();
        }

        public void Hide()
        {
            _targetSlotRect = null;
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void UpdatePositionToTarget()
        {
            if (_targetSlotRect == null || _rectTransform == null || _rectTransform.parent == null) return;

            // 1. 슬롯의 3D 월드 좌표를 툴팁 부모의 로컬 좌표계로 변환 (역산 제어)
            Vector3 targetLocalPos = _rectTransform.parent.InverseTransformPoint(_targetSlotRect.position);

            // 2. 변환된 로컬 좌표에 지정된 오프셋 적용
            _rectTransform.localPosition = targetLocalPos + (Vector3)offset;

            // 3. 월드 스페이스 상의 회전값 일치 (비스듬한 UI 대응)
            _rectTransform.rotation = _targetSlotRect.rotation;
        }
    }
}