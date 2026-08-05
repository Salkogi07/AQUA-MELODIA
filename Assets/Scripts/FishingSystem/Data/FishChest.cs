using System.Collections;
using UnityEngine;
using FishingSystem.Input_Helper;

namespace FishingSystem.Inventory
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class FishChest : MonoBehaviour
    {
        public static FishChest Instance { get; private set; }

        [Header("오브젝트 흔들림 강도")]
        [SerializeField] private float shakeDuration = 0.4f;
        [SerializeField] private float shakeAmount = 0.12f;

        [Header("연결할 인벤토리 UI")]
        [SerializeField] private GameObject chestUiPanel;

        private Vector3 _originLocalPosition;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _originLocalPosition = transform.localPosition;
            
            if (chestUiPanel != null) chestUiPanel.SetActive(false);
        }

        private void OnMouseDown()
        {
            // UI 클릭 영역인 경우 동작 차단
            if (Input.GetMouseButtonDown(0))
            {
                ToggleChestUI();
            }
        }

        public void ToggleChestUI()
        {
            if (chestUiPanel != null)
            {
                chestUiPanel.SetActive(!chestUiPanel.activeSelf);
            }
        }

        public void PlayShakeEffect()
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Random.Range(-shakeAmount, shakeAmount);
                transform.localPosition = _originLocalPosition + new Vector3(xOffset, 0f, 0f);
                yield return null;
            }
            transform.localPosition = _originLocalPosition;
        }
    }
}