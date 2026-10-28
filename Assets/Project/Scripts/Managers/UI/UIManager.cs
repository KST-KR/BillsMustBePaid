using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 UI의 표시 및 갱신을 관리
/// 패널 표시는 GameManager의 게임 상태(EGameState)를 단일 기준으로 삼고,
/// 스테이지 이벤트는 상태가 바뀌지 않는 경우(예: Playing → Playing)의 보정에만 사용
/// 일시정지는 게임 상태와 별개의 겹침(overlay)이므로 현재 화면을 그대로 두고 그 위에 표시
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 싱글톤
    public static UIManager Instance { get; private set; }
    #endregion

    #region 인스펙터
    [Header("화면 패널")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _stageClearPanel;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _growthSkillTreePanel;
    [SerializeField] private GameObject _billPanel;

    [Header("업그레이드 탭 선택 표시")]
    [SerializeField] private GameObject _billTabSelectedImage;
    [SerializeField] private GameObject _skillTreeTabSelectedImage;
    [SerializeField] private GameObject _piggyBankTabSelectedImage;
    [SerializeField] private GameObject _collectionTabSelectedImage;

    // [UI-04-02] 일시정지 메뉴
    [Header("일시정지 메뉴")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _runEndConfirmPanel;
    [SerializeField] private GameObject _mainMenuConfirmPanel;
    // 스테이지 진행 중이 아닐 때는 숨김
    [SerializeField] private GameObject _endRunButton;
    [SerializeField] private string _titleSceneName = "MainMenu";

    [Header("플레이어 스태미나")]
    [SerializeField] private PlayerStamina _playerStamina;

    [Header("스태미나 UI")]
    [SerializeField] private GameObject _staminaPanel;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private Image _staminaBar;

    [Header("결과 화면 재화 UI")]
    [SerializeField] private TMP_Text _stageRewardGoldText;
    [SerializeField] private TMP_Text _resultTotalGoldText;

    [Header("업그레이드 화면 재화 UI")]
    [SerializeField] private TMP_Text _upgradeGoldText;
    [SerializeField] private TMP_Text _upgradeLegacyPointText;
    #endregion

    #region 내부 변수
    private int _currentStage;

    // 구독 시점의 참조를 보관하여, 파괴 순서와 상관없이 정확히 구독을 해제
    private GameManager _gameManager;
    private StageManager _stageManager;
    private CurrencyManager _currencyManager;
    private StageRewardManager _stageRewardManager;

    private bool _isSubscribed;
    #endregion

    #region 이벤트 함수
    /// <summary>
    /// UIManager 싱글톤을 등록
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetSelectedUpgradeTab(null);
    }

    /// <summary>
    /// 이벤트 구독과 현재 상태의 UI를 갱신
    /// (다른 매니저의 Awake가 끝난 뒤인 Start에서 구독)
    /// </summary>
    private void Start()
    {
        // 중복 인스턴스는 Destroy 예약 상태에서도 Start가 호출될 수 있으므로 차단
        if (Instance != this)
        {
            return;
        }

        SubscribeEvents();
        RefreshCurrentUI();
    }

    /// <summary>
    /// [UI-04-01] ESC 키 처리
    /// 확인 창이 열러 있으면 닫기, 설정 패널이 열려 있으면 닫기 그외에는 일시정지/재개 전환
    /// Update는 일시정지(timeScale 0) 중에도 호출되므로 재개도 이 키로 가능
    /// 클리어, 게임 오버, 타이틀 상태에서는 GameManager가 일시정지 허용X
    /// </summary>
    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (IsPanelOpen(_runEndConfirmPanel))
        {
            SetPanelActive(_runEndConfirmPanel, false);
            return;
        }

        if (IsPanelOpen(_mainMenuConfirmPanel))
        {
            SetPanelActive(_mainMenuConfirmPanel, false);
            return;
        }

        if (IsPanelOpen(_settingsPanel))
        {
            SetPanelActive(_settingsPanel, false);
            return;
        }

        GameManager gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }

    /// <summary>
    /// UIManager가 파괴될 때 구독과 싱글톤 참조를 해제합
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 구독
    /// </summary>
    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        _gameManager = GameManager.Instance;
        _stageManager = StageManager.Instance;
        _currencyManager = CurrencyManager.Instance;
        _stageRewardManager = StageRewardManager.Instance;

        if (_gameManager != null)
        {
            _gameManager.OnGameStateChanged += HandleGameStateChanged;
            _gameManager.OnPauseChanged += HandlePauseChanged;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted += HandleStageStarted;
            _stageManager.OnStageFailed += HandleStageFailed;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged += HandleBalanceChanged;
        }

        if (_stageRewardManager != null)
        {
            _stageRewardManager.OnStageRewardSettled += HandleStageRewardSettled;
        }

        if (_playerStamina != null)
        {
            _playerStamina.OnStaminaChanged += HandleStaminaChanged;

            // 구독 이전에 변경된 값까지 초기 UI에 반영
            UpdateStaminaUI(_playerStamina.Current, _playerStamina.Max);
        }

        _isSubscribed = true;
    }

    /// <summary>
    /// 구독 해제 (구독 때 보관한 참조 기준)
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_gameManager != null)
        {
            _gameManager.OnGameStateChanged -= HandleGameStateChanged;
            _gameManager.OnPauseChanged -= HandlePauseChanged;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
            _stageManager.OnStageFailed -= HandleStageFailed;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        if (_stageRewardManager != null)
        {
            _stageRewardManager.OnStageRewardSettled -= HandleStageRewardSettled;
        }

        if (_playerStamina != null)
        {
            _playerStamina.OnStaminaChanged -= HandleStaminaChanged;
        }

        _gameManager = null;
        _stageManager = null;
        _currencyManager = null;
        _stageRewardManager = null;

        _isSubscribed = false;
    }

    /// <summary>
    /// 게임 상태 변경 이벤트 처리
    /// </summary>
    private void HandleGameStateChanged(EGameState state)
    {
        ApplyState(state);
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (isPaused)
        {
            ShowPauseUI();
        }
        else
        {
            HidePauseUI();
        }
    }

    /// <summary>
    /// 게임 상태에 맞는 패널만 표시
    /// 일시정지는 상태가 아니므로 여기서 다루지 않음.
    /// </summary>
    private void ApplyState(EGameState state)
    {
        switch (state)
        {
            case EGameState.Title:
                // 타이틀 화면은 별도 씬에서 관리
                HideAllPanels();
                break;

            case EGameState.Playing:
                ShowGameUI();
                break;

            case EGameState.StageClear:
                ShowStageClearUI();
                break;

            case EGameState.GameOver:
                ShowGameOverUI();
                break;
        }
    }

    /// <summary>
    /// 스테이지 시작 시 현재 상태 기준으로 UI를 다시 적용
    /// 런 시작 직후처럼 상태가 이미 Playing이면 상태 변경 이벤트가 오지 않으므로
    /// 스태미나 UI 표시를 이 이벤트에서 보정
    /// </summary>
    private void HandleStageStarted(int stageNumber)
    {
        UpdateStageUI(stageNumber);

        if (_gameManager != null)
        {
            ApplyState(_gameManager.State);
        }
        else
        {
            ShowGameUI();
        }

        if (_playerStamina != null)
        {
            UpdateStaminaUI(_playerStamina.Current, _playerStamina.Max);
        }
    }

    /// <summary>
    /// 스테이지 실패 시 스태미나 UI를 숨김
    /// (이후 GameOver 상태 변경으로 게임 오버 UI가 표시됨)
    /// </summary>
    private void HandleStageFailed()
    {
        SetStaminaUIVisible(false);
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        switch (type)
        {
            case ECurrencyType.Gold:
                UpdateMoneyUI(currentAmount);
                break;
            case ECurrencyType.LegacyPoint:
                UpdateLegacyPointUI(currentAmount);
                break;
        }
    }

    /// <summary>
    /// 스테이지 보상 정산 결과를 UI에 반영합니다.
    /// </summary>
    private void HandleStageRewardSettled(int rewardGold, int totalGold)
    {
        UpdateStageRewardGoldUI(rewardGold);
        UpdateResultTotalGoldUI(totalGold);

        // CurrencyManager의 실제 잔고와 UI를 동기화
        RefreshCurrencyUI();
    }

    /// <summary>
    /// 플레이어 스태미나 변경 이벤트 처리
    /// </summary>
    private void HandleStaminaChanged(float currentStamina, float maxStamina)
    {
        UpdateStaminaUI(currentStamina, maxStamina);
    }

    private bool IsStageActive()
    {
        return _stageManager != null && _stageManager.IsStageActive;
    }

    private bool IsPaused()
    {
        GameManager gameManager = GameManager.Instance;

        return gameManager != null && gameManager.IsPaused;
    }

    /// <summary>
    /// 일시정지 메뉴에서 '런 종료'(현재 스테이지 종료)를 할 수 있는지
    /// 스테이지가 진행 중인 Playing 상태에서만 가능
    /// </summary>
    /// <returns></returns>
    private bool CanEndRunFromPause()
    {
        GameManager gameManager = GameManager.Instance;

        return gameManager != null && gameManager.State == EGameState.Playing && IsStageActive();
    }

    private bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }

    private void SetStaminaUIVisible(bool isVisible)
    {
        SetPanelActive(_staminaPanel, isVisible);
    }

    /// <summary>
    /// 패널 온오프 관리
    /// </summary>
    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private void ShowUpgradeSection(GameObject selectedSection)
    {
        if (_upgradePanel == null || selectedSection == null)
        {
            Debug.LogWarning("업그레이드 패널 또는 선택한 하위 패널이 연결되지 않음.");
            return;
        }

        SetPanelActive(_stageClearPanel, false);
        SetPanelActive(_upgradePanel, true);
        SetPanelActive(_growthSkillTreePanel, selectedSection == _growthSkillTreePanel);
        SetPanelActive(_billPanel, selectedSection == _billPanel);
        RefreshCurrencyUI();
    }

    private void SetSelectedUpgradeTab(GameObject selectedImage)
    {
        SetPanelActive(_billTabSelectedImage, selectedImage == _billTabSelectedImage);
        SetPanelActive(_skillTreeTabSelectedImage, selectedImage == _skillTreeTabSelectedImage);
        SetPanelActive(_piggyBankTabSelectedImage, selectedImage == _piggyBankTabSelectedImage);
        SetPanelActive(_collectionTabSelectedImage, selectedImage == _collectionTabSelectedImage);
    }

    private void RefreshCurrencyUI()
    {
        if (_currencyManager == null)
        {
            return;
        }

        UpdateMoneyUI(_currencyManager.Gold);
        UpdateLegacyPointUI(_currencyManager.LegacyPoints);
    }

    /// <summary>
    /// 진행 상황 저장
    ///  TODO: SaveManager의 저장 메서드 연결 
    /// </summary>
    /// <returns></returns>
    private bool SaveProgress()
    {
        return true;
    }

    /// <summary>
    /// [UI-04-02] 메인 메뉴 이동: 런을 종료 상태로 만들고 타이틀 씬으로 이동
    /// 확인 창의 '예' 버튼에서만 호출
    /// </summary>
    private void MoveToMainMenu()
    {
        if (string.IsNullOrEmpty(_titleSceneName))
        {
            Debug.LogError("타이틀 씬 이름이 설정도지 않아 메인메뉴로 이동할 수 없음.");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToTitle();
        }

        SceneManager.LoadScene(_titleSceneName);
    }
    
    #endregion

    #region 커스텀 메서드 (Public)
    /// <summary>
    /// 현재 게임 상태와 보유 재화를 기준으로 UI를 초기화
    /// 불러오기(Load) 직후에도 호출해서 UI를 동기화할 수 있음
    /// </summary>
    public void RefreshCurrentUI()
    {
        if (_gameManager != null)
        {
            ApplyState(_gameManager.State);

            if (_gameManager.IsPaused)
            {
                ShowPauseUI();
            }
        }
        else
        {
            // 게임 상태를 확인할 수 없을 때는 UI를 표시하지 않음.
            HideAllPanels();
        }

        RefreshCurrencyUI();
    }

    /// <summary>
    /// 현재 게임 플레이 UI를 표시
    /// </summary>
    public void ShowGameUI()
    {
        HideAllPanels();

        SetStaminaUIVisible(IsStageActive());
    }

    /// <summary>
    /// [UI-04-01] 일시정지 UI를 현재 화면 위에 겹쳐서 표시
    /// 플레이, 클리어, 게임 오버 화면과 그 하위 화면은 그대로 두어 재개하면 같은 화면으로 돌아감
    /// '런 종료' 버튼은 스테이지 진행 중에만 표시
    /// </summary>
    public void ShowPauseUI()
    {
        SetPanelActive(_settingsPanel, false);
        SetPanelActive(_runEndConfirmPanel, false);
        SetPanelActive(_mainMenuConfirmPanel, false);

        SetPanelActive(_endRunButton, CanEndRunFromPause());
        SetPanelActive(_pausePanel, true);

        // 스테이지 결과 화면에서는 일시정지 패널을 가장 앞에 표시
        if (_pausePanel != null && _gameManager != null && _gameManager.State == EGameState.StageClear)
        {
            _pausePanel.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 일시정지 UI와 그 위의 설정 패널, 확인 창을 닫음 (현재 화면은 그대로 유지)
    /// </summary>
    public void HidePauseUI()
    {
        SetPanelActive(_pausePanel, false);
        SetPanelActive(_settingsPanel, false);
        SetPanelActive(_runEndConfirmPanel, false);
        SetPanelActive(_mainMenuConfirmPanel, false);
    }

    /// <summary>
    /// 스테이지 종료 후 결과 화면을 표시
    /// </summary>
    public void ShowStageClearUI()
    {
        HideAllPanels();

        SetPanelActive(_stageClearPanel, true);
        RefreshCurrencyUI();
    }

    /// <summary>
    /// 파산 선고 화면을 표시
    /// </summary>
    public void ShowGameOverUI()
    {
        HideAllPanels();

        SetPanelActive(_gameOverPanel, true);
    }

    /// <summary>
    /// 관리 대상 UI를 모두 숨김.
    /// 화면 상태가 바뀔 떄 호츨, GameManager가 일시정지도 해제하므로 일시정지 패널이 남지 않음.
    /// </summary>
    public void HideAllPanels()
    {
        HidePauseUI();
        SetPanelActive(_stageClearPanel, false);
        SetPanelActive(_gameOverPanel, false);
        SetPanelActive(_upgradePanel, false);
        SetStaminaUIVisible(false);
    }

    /// <summary>
    /// 현재 스태미나와 최대 스태미나를 UI에 반영
    /// </summary>
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        if (_staminaText != null)
        {
            _staminaText.text = $"{currentStamina:0} / {maxStamina:0}";
        }

        if (_staminaBar != null)
        {
            _staminaBar.fillAmount = maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
        }
    }

    /// <summary>
    /// 업그레이드 화면의 보유 재화를 UI에 반영
    /// </summary>
    public void UpdateMoneyUI(int amount)
    {
        if (_upgradeGoldText != null)
        {
            _upgradeGoldText.text = amount.ToString();
        }
    }

    /// <summary>
    /// 업그레이드 화면의 보유 레거시 포인트를 UI에 반영
    /// </summary>
    public void UpdateLegacyPointUI(int amount)
    {
        if (_upgradeLegacyPointText != null)
        {
            _upgradeLegacyPointText.text = amount.ToString();
        }
    }

    /// <summary>
    /// 이번 스테이지 획득 골드를 갱신
    /// </summary>
    public void UpdateStageRewardGoldUI(int amount)
    {
        if (_stageRewardGoldText != null)
        {
            _stageRewardGoldText.text = "$ " + amount.ToString();
        }
    }

    /// <summary>
    /// 정산 후 보유 골드를 결과 화면에 표시
    /// </summary>
    public void UpdateResultTotalGoldUI(int amount)
    {
        if (_resultTotalGoldText != null)
        {
            _resultTotalGoldText.text = amount.ToString();
        }
    }

    /// <summary>
    /// 스테이지 번호는 내부 진행 정보로만 사용
    /// 플레이어에게는 표시하지 않음.
    /// </summary>
    public void UpdateStageUI(int stageNumber)
    {
        if (stageNumber < 0)
        {
            Debug.LogWarning("스테이지 번호는 음수가 될 수 없음.");
            return;
        }

        _currentStage = stageNumber;
    }

    /// <summary>
    /// 저금통 파괴에 따른 레벨 수치를 갱신
    /// </summary>
    public void UpdateLevelUI(int level)
    {
        // TODO: 레벨 UI 갱신
        // 레벨 UI 구현 단계에서 연결
    }

    /// <summary>
    /// 청구서 금액을 갱신
    /// </summary>
    public void UpdateBillUI(int amount)
    {
        // TODO: 청구서 UI 갱신
        // 청구서 UI 구현 단계에서 연결
    }

    /// <summary>
    /// 성장 및 보상 선택 UI를 표시합니다.
    /// </summary>
    public void ShowRewardUI()
    {
        // TODO: 성장 및 보상 UI 표시
        // 보상 선택 UI 구현 단계에서 연결
    }

    /// <summary>
    /// 스테이지 클리어 화면에서 다음 스테이지를 시작
    /// </summary>
    public void OnContinueButtonClicked()
    {
        if (_stageManager == null)
        {
            _stageManager = StageManager.Instance;
        }

        if (_stageManager == null)
        {
            Debug.LogError("StageManager를 찾을 수 없어 다음 스테이지를 시작할 수 없음.");
            return;
        }

        _stageManager.NextStage();
    }

    /// <summary>
    /// [GROW-02-01] 성장 트리 화면 전환
    /// 스테이지 결과 패널은 비활성 처리
    /// 업그레이드 화면의 성장 트리 표시
    /// </summary>
    public void OnUpgradeButtonClicked()
    {
        HideAllPanels();
        ShowUpgradeSection(_growthSkillTreePanel);
        SetSelectedUpgradeTab(_skillTreeTabSelectedImage);
    }

    /// <summary>
    /// [BILL-03-02] 스테이지 결과에서 청구서 패널 전환
    /// 스테이지 결과 패널은 비활성 처리
    /// 업그레이드 화면의 청구서 내용 표시
    /// </summary>
    public void OnBillButtonClicked()
    {
        HideAllPanels();
        ShowUpgradeSection(_billPanel);
        SetSelectedUpgradeTab(_billTabSelectedImage);
    }

    /// <summary>
    /// [GROW-02-01] 스킬 트리 탭 활성화
    /// 청구서 패널은 비활성 처리
    /// 업그레이드 화면의 성장 트리 표시
    /// </summary>
    public void OnSkillTreeTabButtonClicked()
    {
        ShowUpgradeSection(_growthSkillTreePanel);
        SetSelectedUpgradeTab(_skillTreeTabSelectedImage);
    }

    /// <summary>
    /// [BILL-03-02] 청구서 탭 활성화
    /// 성장 트리 패널은 비활성 처리
    /// 업그레이드 화면의 청구서 내용 표시
    /// </summary>
    public void OnBillTabButtonClicked()
    {
        ShowUpgradeSection(_billPanel);
        SetSelectedUpgradeTab(_billTabSelectedImage);
    }

    /// <summary>
    /// [UI-04-01] 인게임 일시정지 버튼 (ESC 키와 같은 동작, HUD에 버튼이 있을 때만 연결)
    /// </summary>
    public void OnPauseButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
    }

    /// <summary>
    /// [UI-04-02] 계속하기: 일시정지를 해제하고 진행 중이던 플레이로 복귀
    /// </summary>
    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    /// <summary>
    /// [UI-04-02] 설정: 설정 패널을 열기(일시정지 패널 위에 표시)
    /// </summary>
    public void OnSettingsButtonClicked()
    {
        if (_settingsPanel == null)
        {
            Debug.LogWarning("설정 패널이 연결되지 않음.");
            return;
        }

        SetPanelActive(_settingsPanel, true);

        if (_settingsPanel != null)
        {
            _settingsPanel.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// [UI-04-02] 설정 패널의 닫기 버튼: 일시정지 메뉴로 돌아감.
    /// </summary>
    public void OnSettingsCloseButtonClicked()
    {
        SetPanelActive(_settingsPanel, false);
    }

    /// <summary>
    /// [UI-04-02] 런 종료: 확인 창을 표시
    /// 스테이지가 진행 중일 때만 동작, 클리와 게임오버 화면에서는 버튼 자체가 숨겨짐.
    /// </summary>
    public void OnRunEndButtonClicked()
    {
        if (!IsPaused() || !CanEndRunFromPause())
        {
            return;
        }

        SetPanelActive(_runEndConfirmPanel, true);

        if (_runEndConfirmPanel != null)
        {
            _runEndConfirmPanel.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// [UI-04-02] 런 종료 확인을 확정하고 현재 스테이지를 종료
    /// 스테이지 종료시 GameManager가 일시정지를 해제하고 스테이지 종료 UI가 표시
    /// </summary>
    public void OnRunEndConfirmYesButtonClicked()
    {
        if (_stageManager == null)
        {
            _stageManager = StageManager.Instance;
        }

        if (_stageManager == null)
        {
            Debug.LogError("StageManager를 찾을 수 없어 스테이지를 종료할 수 없음.");
            SetPanelActive(_runEndConfirmPanel, false);
            return;
        }

        if (!_stageManager.IsStageActive)
        {
            Debug.LogWarning("진행 중인 스테이지가 없어 종료할 수 없음.");
            SetPanelActive(_runEndConfirmPanel, false);
            return;
        }

        _stageManager.ClearStage();
    }

    /// <summary>
    /// 런 종료 확인을 취소하고 일시정지 메뉴로 돌아가기
    /// </summary>
    public void OnRunEndCancelButtonClicked()
    {
        SetPanelActive(_runEndConfirmPanel, false);
    }

    /// <summary>
    /// [UI-04-02] 메인 메뉴: 진행 상황을 저장한 뒤 확인 창을 표시
    /// 창을 열기 전에 저장을 먼저 수행
    /// </summary>
    public void OnMainMenuConfirmButtonClicked()
    {
        SaveProgress();
        SetPanelActive(_mainMenuConfirmPanel, true);

        if (_mainMenuConfirmPanel != null)
        {
            _mainMenuConfirmPanel.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 메인 메뉴 이동을 취소하고 일시정지 메뉴로 돌아가기
    /// </summary>
    public void OnMainMenuCancelButtonClicked()
    {
        SetPanelActive(_mainMenuConfirmPanel, false);
    }

    /// <summary>
    /// 메인 메뉴 이동을 확정합니다.
    /// </summary>
    public void OnMainMenuConfirmYesButtonClicked()
    {
        SetPanelActive(_mainMenuConfirmPanel, false);
        MoveToMainMenu();
    }

    /// <summary>
    /// [UI-04-02] 종료: 게임을 종료
    /// </summary>
    public void OnQuitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
    #endregion
}
