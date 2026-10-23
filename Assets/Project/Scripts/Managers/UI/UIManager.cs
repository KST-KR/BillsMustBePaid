using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 UI의 표시 및 갱신을 관리합니다.
/// 패널 표시는 GameManager의 게임 상태(EGameState)를 단일 기준으로 삼고,
/// 스테이지 이벤트는 상태가 바뀌지 않는 경우(예: Playing → Playing)의 보정에만 사용
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

    // 구독 시점의 참조를 보관하여, 파괴 순서와 상관없이 정확히 구독을 해제
    private GameManager _gameManager;
    private StageManager _stageManager;
    private CurrencyManager _currencyManager;
    private StageRewardManager _stageRewardManager;

    private bool _isSubscribed;
    #endregion

    #region 이벤트 함수

    /// <summary>
    /// UIManager 싱글톤을 등록합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
    /// UIManager가 파괴될 때 구독과 싱글톤 참조를 해제합니다.
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

    /// <summary>
    /// 게임 상태에 맞는 패널만 표시
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

            case EGameState.Paused:
                ShowPauseUI();
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

    private void RefreshCurrencyUI()
    {
        if (_currencyManager == null)
        {
            return;
        }

        UpdateMoneyUI(_currencyManager.Gold);
        UpdateLegacyPointUI(_currencyManager.LegacyPoints);
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
        }
        else
        {
            // 게임 상태를 확인할 수 없을 때는 UI를 표시하지 않습니다.
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
    /// 일시정지 UI를 표시
    /// 스테이지가 진행 중이면 스태미나 UI도 유지
    /// </summary>
    public void ShowPauseUI()
    {
        HideAllPanels();

        SetPanelActive(_pausePanel, true);
        SetStaminaUIVisible(IsStageActive());
    }

    /// <summary>
    /// 스테이지 종료 후 결과 화면을 표시합니다.
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
    /// </summary>
    public void HideAllPanels()
    {
        SetPanelActive(_pausePanel, false);
        SetPanelActive(_stageClearPanel, false);
        SetPanelActive(_gameOverPanel, false);
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
        // 현재 플레이어에게 표시할 스테이지 번호 UI는 없음.
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
    /// 스테이지 클리어 화면에서 다음 스테이지를 시작합니다.
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

    #endregion
}
