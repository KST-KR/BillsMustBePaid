using UnityEngine;

/// <summary>
/// [UI-01-01]
/// 메인 메뉴 UI 표시 및 패널 전환 관리
/// </summary>
public class MainMenuUI : MonoBehaviour
{
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

    #region 이벤트 함수
    private void Start()
    {
        // 실제 저장 시스템과 연결할 부분
        bool hasSaveData = CheckSaveData();

        SetSaveDataUI(hasSaveData);
        ShowPanel(_mainMenuPanel);
    }
    #endregion

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

    private void ShowPanel(GameObject targetPanel)
    {
        _mainMenuPanel.SetActive(targetPanel == _mainMenuPanel);
        _loadPanel.SetActive(targetPanel == _loadPanel);
        _settingsPanel.SetActive(targetPanel == _settingsPanel);
    }

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

        // 가장 최근 저장 슬롯 확인
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

        // 게임 씬에 이어하기 정보를 전달
        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.LoadGame, slotIndex);

        Debug.Log($"이어하기 시작: 슬롯 {slotIndex}");

        SceneTransitionManager.Instance.LoadGameScene();
    }
}