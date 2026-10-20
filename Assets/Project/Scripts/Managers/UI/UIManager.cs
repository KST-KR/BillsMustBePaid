using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 UI의 표시 및 갱신을 관리합니다.
/// 외부 시스템에서 전달받은 정보를 UI에 반영합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 싱글톤

    public static UIManager Instance { get; private set; }

    #endregion

    #region 인스펙터
    [Header("화면 패널")]
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _stageClearPanel;
    [SerializeField] private GameObject _gameOverPanel;

    [Header("상황별 UI")]
    [SerializeField] private GameObject _staminaPanel;
    [SerializeField] private GameObject _resultCurrencyPanel;

    [Header("스태미나 UI")]
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private Image _staminaBar;

    [Header("결과 화면 재화 UI")]
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _legacyPointText;
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
    /// </summary>
    private void Start()
    {
        SubscribeEvents();
        RefreshCurrentUI();
    }

    /// <summary>
    /// UIManager가 파괴될 때 싱글톤 참조를 해제합니다.
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += HandleStageStarted;
            StageManager.Instance.OnStageChanged += HandleStageCleared;
            StageManager.Instance.OnStageFailed += HandleStageFailed;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
        }
    }

    /// <summary>
    /// 구독 해제
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted -= HandleStageStarted;
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
            StageManager.Instance.OnStageFailed -= HandleStageFailed;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
        }
    }

    /// <summary>
    /// Handle (이벤트 처리)
    /// </summary>
    /// <param name="state"></param>
    private void HandleGameStateChanged(EGameState state)
    {
        switch (state)
        {
            case EGameState.Title:
                ShowTitleUI();
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

    private void HandleStageStarted(int stageNumber)
    {
        ShowGameUI();
        SetStaminaUIVisible(true);
    }

    private void HandleStageCleared(int stageNumber)
    {
        SetStaminaUIVisible(false);
        ShowStageClearUI();
    }

    private void HandleStageFailed()
    {
        SetStaminaUIVisible(false);
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        if (type == ECurrencyType.Gold)
        {
            if (_goldText != null)
            {
                _goldText.text = currentAmount.ToString();
            }
        }
        else if (type == ECurrencyType.LegacyPoint)
        {
            if (_legacyPointText != null)
            {
                _legacyPointText.text = currentAmount.ToString();
            }
        }
    }

    /// <summary>
    /// 초기 UI 갱신
    /// </summary>
    private void RefreshCurrentUI()
    {
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.State);
        }
        else
        {
            ShowTitleUI();
        }

        bool isStageActive = StageManager.Instance != null && StageManager.Instance.IsStageActive;

        SetStaminaUIVisible(isStageActive);

        if (CurrencyManager.Instance != null)
        {
            UpdateMoneyUI(CurrencyManager.Instance.Gold);
            UpdateLegacyPointUI(CurrencyManager.Instance.LegacyPoints);
        }
        
        // PlayerStamina의 실제 참조 연결은 스태미나 UI 연결 단계에서 진행
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

    private void SetStaminaUIVisible(bool isVisible)
    {
        SetPanelActive(_staminaPanel, isVisible);
    }

    private void RefreshCurrencyUI()
    {
        if (CurrencyManager.Instance == null)
        {
            return;
        }

        UpdateMoneyUI(CurrencyManager.Instance.Gold);
        UpdateLegacyPointUI(CurrencyManager.Instance.LegacyPoints);
    }
    #endregion

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 타이틀 UI를 표시합니다.
    /// </summary>
    public void ShowTitleUI()
    {
        HideAllPanels();

        SetPanelActive(_titlePanel, true);
    }

    /// <summary>
    /// 게임 플레이 UI를 표시합니다.
    /// </summary>
    public void ShowGameUI()
    {
        HideAllPanels();

        SetPanelActive(_gamePanel, true);

        bool isStageActive = StageManager.Instance != null && StageManager.Instance.IsStageActive;

        SetStaminaUIVisible(isStageActive);

        SetPanelActive(_resultCurrencyPanel, false);
    }

    /// <summary>
    /// 일시정지 UI를 표시합니다.
    /// </summary>
    public void ShowPauseUI()
    {
        SetPanelActive(_pausePanel, true);
    }

    /// <summary>
    /// 스테이지 클리어 UI를 표시합니다.
    /// </summary>
    public void ShowStageClearUI()
    {
        HideAllPanels();

        SetPanelActive(_stageClearPanel, true);
        SetPanelActive(_resultCurrencyPanel, true);

        RefreshCurrencyUI();
    }

    /// <summary>
    /// 게임 오버 UI를 표시합니다.
    /// </summary>
    public void ShowGameOverUI()
    {
        HideAllPanels();

        SetPanelActive(_gameOverPanel, true);
        SetPanelActive(_resultCurrencyPanel, true);

        RefreshCurrencyUI();
    }

    /// <summary>
    /// 전체 UI 패널을 숨깁니다.
    /// </summary>
    public void HideAllPanels()
    {
        SetPanelActive(_titlePanel, false);
        SetPanelActive(_gamePanel, false);
        SetPanelActive(_pausePanel, false);
        SetPanelActive(_stageClearPanel, false);
        SetPanelActive(_gameOverPanel, false);

        SetStaminaUIVisible(false);
        SetPanelActive(_resultCurrencyPanel, false);
    }

    /// <summary>
    /// 현재 스태미나와 최대 스태미나를 UI에 반영합니다.
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
    /// 보유 재화를 UI에 반영합니다.
    /// </summary>
    public void UpdateMoneyUI(int amount)
    {
        if (_goldText != null)
        {
            _goldText.text = amount.ToString();
        }
    }

    /// <summary>
    /// 레거시 포인트를 UI에 반영합니다.
    /// </summary>
    public void UpdateLegacyPointUI(int amount)
    {
        if (_legacyPointText != null)
        {
            _legacyPointText.text = amount.ToString();
        }
    }

    /// <summary>
    /// 현재 스테이지 번호를 UI에 반영합니다.
    /// </summary>
    public void UpdateStageUI(int stageNumber)
    {
        // TODO: 스테이지 UI 갱신
        // 스테이지 번호는 내부 진행 정보로만 사용하며 플레이어에게 표시하지 않음.
    }

    /// <summary>
    /// 저금통 파괴에 따른 레벨 수치를 UI에 반영합니다.
    /// </summary>
    public void UpdateLevelUI(int level)
    {
        // TODO: 레벨 UI 갱신
        // 레벨 UI 구현 단계에서 연결
    }

    /// <summary>
    /// 청구서 금액을 UI에 반영합니다.
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

    #endregion
}

