using UnityEngine;

/// <summary>
/// [UI-01-01]
/// 메인 메뉴 UI 표시 및 패널 전환 관리
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    #region 인스펙터
    [Header("항상 표시할 버튼")]
    [SerializeField] private GameObject _settingsButton;
    [SerializeField] private GameObject _exitButton;

    [Header("저장 데이터가 없을 때 표시할 버튼")]
    [SerializeField] private GameObject _newGameButton;

    [Header("저장 데이터가 있을 때 표시할 버튼")]
    [SerializeField] private GameObject _continueButton;
    [SerializeField] private GameObject _skillTreeButton;
    [SerializeField] private GameObject _loadOrNewGameButton;

    [Header("메뉴 패널")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _loadPanel;
    [SerializeField] private GameObject _settingsPanel;

    [Header("삭제 확인 UI")]
    [SerializeField] private SaveSlotDeleteConfirmUI _deleteConfirmUI;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if (_deleteConfirmUI != null)
        {
            _deleteConfirmUI.OnSlotDeleted += HandleSlotDeleted;
        }
    }

    private void Start()
    {
        RefreshSaveDataUI();
        ShowPanel(_mainMenuPanel);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        // 가장 위에 열린 패널부터 닫기
        if (_deleteConfirmUI != null && _deleteConfirmUI.TryClose())
        {
            return;
        }

        if (_loadPanel.activeSelf)
        {
            CloseLoadPanel();
            return;
        }

        if (_settingsPanel.activeSelf)
        {
            CloseSettingsPanel();
        }
    }

    private void OnDisable()
    {
        if (_deleteConfirmUI != null)
        {
            _deleteConfirmUI.OnSlotDeleted -= HandleSlotDeleted;
        }
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 저장 데이터 유무를 확인하고 메인 메뉴 버튼을 갱신
    /// </summary>
    private void RefreshSaveDataUI()
    {
        bool hasSaveData = CheckSaveData();

        SetSaveDataUI(hasSaveData);
    }

    private bool CheckSaveData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return false;
        }

        // 저장 슬롯 1~3 중 하나라도 데이터가 있는지 확인
        for (int slotIndex = 1; slotIndex <= 3; slotIndex++)
        {
            if (SaveManager.Instance.HasSaveData(slotIndex))
            {
                Debug.Log($"저장 데이터 확인: 슬롯 {slotIndex}");
                return true;
            }
        }

        Debug.Log("저장 데이터 없음.");
        return false;
    }

    /// <summary>
    /// 저장 데이터 유무에 따라 버튼 표시
    /// </summary>
    private void SetSaveDataUI(bool hasSaveData)
    {
        // 항상 표시할 버튼
        _settingsButton.SetActive(true);
        _exitButton.SetActive(true);

        // 저장 데이터가 없으면 새 게임 버튼 표시
        _newGameButton.SetActive(!hasSaveData);

        // 저장 데이터가 있으면 관련 버튼 표시
        _continueButton.SetActive(hasSaveData);
        _skillTreeButton.SetActive(hasSaveData);
        _loadOrNewGameButton.SetActive(hasSaveData);
    }

    /// <summary>
    /// 저장 슬롯 삭제 완료 이벤트
    /// </summary>
    /// <param name="deletedSlotIndex"></param>
    private void HandleSlotDeleted(int deletedSlotIndex)
    {
        Debug.Log($"저장 슬롯 {deletedSlotIndex} 삭제 완료. 메인 메뉴 UI 갱신");

        RefreshSaveDataUI();
    }

    private void ShowPanel(GameObject targetPanel)
    {
        _mainMenuPanel.SetActive(targetPanel == _mainMenuPanel);
        _loadPanel.SetActive(targetPanel == _loadPanel);
        _settingsPanel.SetActive(targetPanel == _settingsPanel);
    }
    #endregion

    #region 커스텀 (public)
    public void OpenLoadPanel()
    {
        ShowPanel(_loadPanel);
    }

    public void CloseLoadPanel()
    {
        ShowPanel(_mainMenuPanel);
    }

    public void OpenSettingsPanel()
    {
        ShowPanel(_settingsPanel);
    }

    public void CloseSettingsPanel()
    {
        ShowPanel(_mainMenuPanel);
    }

    public void OnNewGameButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager를 찾을 수 없음.");
            return;
        }

        // 처음 시작하는 새 게임은 슬롯 1번을 사용
        const int defaultSlotIndex = 1;

        if (SaveManager.Instance.HasSaveData(defaultSlotIndex))
        {
            Debug.LogWarning("슬롯 1번에 기존 저장 데이터가 있습니다.");
            return;
        }

        if (!SaveManager.Instance.SetCurrentSlotIndex(defaultSlotIndex))
        {
            return;
        }

        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.NewGame, defaultSlotIndex);

        SceneTransitionManager.Instance.LoadGameScene();
    }

    public void OnContinueButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager를 찾을 수 없음.");
            return;
        }

        // 가장 최근에 플레잉한 저장 슬롯 확인
        int slotIndex = SaveManager.Instance.GetMostRecentlyPlayedSlotIndex();

        if (slotIndex < 1 || slotIndex > 3)
        {
            Debug.LogWarning("이어하기에 사용할 수 있는 저장 데이터가 없음.");
            return;
        }

        if (!SaveManager.Instance.SetCurrentSlotIndex(slotIndex))
        {
            return;
        }

        // 일반 이어하기 모드로 게임 씬에 전달
        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.LoadGame, slotIndex);

        Debug.Log($"이어하기 진입: 슬롯 {slotIndex}");

        SceneTransitionManager.Instance.LoadGameScene();
    }

    public void OnSkillTreeButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager를 찾을 수 없음.");
            return;
        }

        // 가장 최근에 플레이한 저장 슬롯 확인
        int slotIndex = SaveManager.Instance.GetMostRecentlyPlayedSlotIndex();

        if (slotIndex < 1 || slotIndex > 3)
        {
            Debug.LogWarning("스킬트리를 열 수 있는 저장 데이터가 없음.");
            return;
        }

        if (!SaveManager.Instance.SetCurrentSlotIndex(slotIndex))
        {
            return;
        }

        // 스킬트리 전용 진입 모드로 게임 씬에 전달
        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.OpenSkillTree, slotIndex);

        Debug.Log($"스킬트리 진입: 슬롯 {slotIndex}");

        SceneTransitionManager.Instance.LoadGameScene();
    }
    #endregion

}