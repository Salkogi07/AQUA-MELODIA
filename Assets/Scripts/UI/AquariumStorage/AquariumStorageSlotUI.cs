using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FishingSystem.Fish;
using FishingSystem.Data;
using TMPro;

namespace FishingSystem.House
{
    // [추가] 이 스크립트가 붙으면 CanvasGroup 컴포넌트가 자동으로 추가되도록 보장합니다.
    [RequireComponent(typeof(CanvasGroup))]
    public class AquariumStorageSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI 표시 요소")]
        [SerializeField] private Image fishIcon;
        [SerializeField] private TMP_Text nameText;    // 이름 (등급) 표시
        [SerializeField] private TMP_Text infoText;    // 품질 및 길이 표시

        private FishData _fishData;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Vector3 _startPosition;
        private Transform _originalParent;
        private Aquarium _targetAquarium;

        public FishData CurrentFishData => _fishData;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            if (fishIcon != null)
            {
                fishIcon.type = Image.Type.Simple;
                fishIcon.preserveAspect = true;
            }
        }

        public void Setup(FishData fish, Canvas canvas, Aquarium aquarium)
        {
            _fishData = fish;
            _canvas = canvas;
            _targetAquarium = aquarium;
            
            if (fish != null)
            {
                if (fishIcon != null) fishIcon.sprite = fish.Data.fishSprite;

                // 이름 설정: 이름 (전설)
                if (nameText != null)
                {
                    string gradeName = GetGradeKorName(fish.Data.grade);
                    nameText.text = $"{fish.Data.fishName} ({gradeName})";
                    nameText.color = GetGradeColor(fish.Data.grade);
                }

                // 설명 설정: 품질: A | 길이: 00.0cm
                if (infoText != null)
                {
                    string qualityName = fish.Quality.ToString().Replace("Grade", "");
                    infoText.text = $"품질: {qualityName} | 길이: {fish.Length:F1}cm";
                }
            }
        }

        private string GetGradeKorName(FishGrade grade)
        {
            return grade switch
            {
                FishGrade.Common => "일반",
                FishGrade.Rare   => "희귀",
                FishGrade.Epic   => "에픽",
                FishGrade.Unique => "유니크",
                FishGrade.Legend => "전설",
                _ => "미정"
            };
        }

        private Color GetGradeColor(FishGrade grade)
        {
            return grade switch
            {
                FishGrade.Legend => new Color(1f, 0.7f, 0f),    // 황금색
                FishGrade.Unique => new Color(0.7f, 0.3f, 1f),  // 보라색
                FishGrade.Epic   => new Color(1f, 0.3f, 0.6f),  // 분홍색
                FishGrade.Rare   => new Color(0.2f, 0.6f, 1f),  // 하늘색
                _ => Color.white
            };
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_fishData == null || _canvasGroup == null) return;

            _startPosition = transform.position;
            _originalParent = transform.parent;
            
            transform.SetParent(_canvas.transform);

            // [안전성 추가] _canvasGroup이 있는지 다시 한번 확인
            _canvasGroup.alpha = 0.7f;
            _canvasGroup.blocksRaycasts = false; 
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1.0f;
                _canvasGroup.blocksRaycasts = true;
            }

            // 1. 마우스 위치를 월드 좌표로 변환 (Z축 보정 중요)
            Vector3 mousePos = Input.mousePosition;
    
            // 카메라와 어항(월드) 사이의 거리를 계산하여 넘겨줘야 정확한 좌표가 나옵니다.
            // 보통 2D에서 카메라는 -10에 있고 어항은 0에 있으므로 거리는 10입니다.
            float distanceFromCamera = Mathf.Abs(Camera.main.transform.position.z - _targetAquarium.transform.position.z);
            mousePos.z = distanceFromCamera; 

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = _targetAquarium.transform.position.z; // 어항과 동일한 Z축 평면으로 고정

            // [디버그 로그] 마우스를 놓은 위치와 콜라이더 인식 여부를 콘솔창에 표시합니다.
            Debug.Log($"드롭 시도 - 마우스 월드 좌표: {worldPos}");

            // 2. 해당 좌표가 어항(Swim Area) 안에 있는지 확인
            if (IsInsideAquarium(worldPos))
            {
                Debug.Log("어항 영역 인식 성공!");
                if (HouseDataManager.Instance.TryAddFishToHouse(_fishData))
                {
                    FishingDataManager.Instance.TryRemoveFish(_fishData);
            
                    if (_targetAquarium != null)
                    {
                        _targetAquarium.SpawnSingleFishAtPosition(_fishData, worldPos);
                    }
            
                    Destroy(gameObject); 
                    return;
                }
                else
                {
                    Debug.LogWarning("어항 데이터에 추가 실패 (공간 부족 등)");
                }
            }
            else
            {
                Debug.Log("어항 영역 인식 실패: 마우스가 콜라이더 밖에 있음");
            }

            // 실패 시 원래 자리로 복구
            transform.SetParent(_originalParent);
            transform.position = _startPosition;
        }

        private bool IsInsideAquarium(Vector3 worldPos)
        {
            if (_targetAquarium == null) return false;

            // 1. Aquarium 스크립트에 이미 연결된 SwimArea를 직접 가져옵니다.
            BoxCollider2D area = _targetAquarium.SwimArea;

            // 만약 연결이 안 되어 있다면 자식에서라도 찾아봅니다.
            if (area == null) area = _targetAquarium.GetComponentInChildren<BoxCollider2D>();
    
            if (area == null)
            {
                Debug.LogError("어항에서 BoxCollider2D를 찾을 수 없습니다!");
                return false;
            }

            // 2. OverlapPoint 대신 Bounds.Contains를 사용합니다. (Z축은 무시)
            // 이 방식이 UI 좌표를 월드로 변환했을 때 훨씬 정확하게 인식됩니다.
            Vector3 boundsPos = new Vector3(worldPos.x, worldPos.y, area.bounds.center.z);
            bool isInside = area.bounds.Contains(boundsPos);

            if (!isInside)
            {
                // 디버깅을 위해 콜라이더의 실제 범위와 마우스 위치를 찍어봅니다.
                Debug.Log($"영역 밖! 마우스:{worldPos}, 콜라이더범위: {area.bounds.min} ~ {area.bounds.max}");
            }

            return isInside;
        }
    }
}