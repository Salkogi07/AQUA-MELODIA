using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Unity 6 기본 텍스트 패키지

namespace FishingSystem.Island
{
    public class IslandUIController : MonoBehaviour
    {
        public static IslandUIController Instance;
        
        [Header("선택된 섬 데이터")]
        [SerializeField] private IslandDataSO targetIsland;

        [Header("기본 정보 UI")]
        [SerializeField] private TMP_Text txtIslandName;
        [SerializeField] private TMP_Text txtIslandDesc;

        [Header("해금 관련 UI")]
        [SerializeField] private GameObject unlockPanel; // 해금되지 않았을 때 보여줄 패널
        [SerializeField] private TMP_Text txtUnlockCost;
        [SerializeField] private Button btnUnlock;

        [Header("기능(씬) 이동 버튼 동적 생성")]
        [SerializeField] private GameObject featuresPanel; // 해금되었을 때 보여줄 구역 패널
        [SerializeField] private Transform buttonContainer; // 버튼들이 배치될 부모 오브젝트 (Grid Layout Group 등 권장)
        [SerializeField] private GameObject buttonPrefab;    // 생성할 버튼 프리팹

        public void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            RefreshUI();
        }

        /// <summary>
        /// 섬의 상태를 파악하여 해금 UI 혹은 구역 이동 버튼들을 갱신하여 띄웁니다.
        /// </summary>
        public void RefreshUI()
        {
            if (targetIsland == null) return;

            // 기본 텍스트 정보 반영
            if (txtIslandName != null) txtIslandName.text = targetIsland.islandName;
            if (txtIslandDesc != null) txtIslandDesc.text = targetIsland.islandDescription;

            bool isUnlocked = IslandManager.Instance.IsUnlocked(targetIsland);

            if (isUnlocked)
            {
                // 해금 완료 상태: 구역 버튼 리스트 생성
                if (unlockPanel != null) unlockPanel.SetActive(false);
                if (featuresPanel != null) featuresPanel.SetActive(true);

                SpawnFeatureButtons();
            }
            else
            {
                // 미해금 상태: 해금 비용 및 조건 제시
                if (unlockPanel != null) unlockPanel.SetActive(true);
                if (featuresPanel != null) featuresPanel.SetActive(false);

                if (txtUnlockCost != null) txtUnlockCost.text = $"{targetIsland.requiredGold} Gold";
                
                if (btnUnlock != null)
                {
                    btnUnlock.interactable = IslandManager.Instance.CanUnlock(targetIsland);
                    btnUnlock.onClick.RemoveAllListeners();
                    btnUnlock.onClick.AddListener(OnUnlockClicked);
                }
            }
        }

        private void OnUnlockClicked()
        {
            if (IslandManager.Instance.TryUnlockIsland(targetIsland))
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// 섬 데이터의 features 리스트를 바탕으로 버튼들을 동적 생성합니다.
        /// </summary>
        // IslandUIController.cs 내부 SpawnFeatureButtons() 함수의 일부분

        private void SpawnFeatureButtons()
        {
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var feature in targetIsland.features)
            {
                if (string.IsNullOrEmpty(feature.sceneName)) continue;

                GameObject newBtnObj = Instantiate(buttonPrefab, buttonContainer);
        
                // 버튼 텍스트 설정 시 새로 추가된 buttonName 적용
                TMP_Text btnText = newBtnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = feature.buttonName;
                }

                Button btn = newBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    string targetScene = feature.sceneName;
                    btn.onClick.AddListener(() => LoadingManager.instance.LoadScene(targetScene));
                }
            }
        }
        
        // 외부에서 수동으로 다른 섬을 선택했을 때 갱신하기 위한 메서드
        public void SetTargetIsland(IslandDataSO newIsland)
        {
            targetIsland = newIsland;
            RefreshUI();
        }
    }
}