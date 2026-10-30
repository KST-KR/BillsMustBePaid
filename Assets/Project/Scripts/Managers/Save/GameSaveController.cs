using UnityEngine;

/// <summary>
/// 게임 진행 데이터를 모아 저장을 요청
/// 스테이지 보상 정산이 완료되면 결과 화면의 진행 데이터를 자동 저장
/// </summary>
public class GameSaveController : MonoBehaviour
{
    #region 싱글톤
    public static GameSaveController Instance { get; private set; }
    #endregion

    #region 내부 변수
    private SaveManager _saveManager;
    private StageManager _stageManager;
    private StageRewardManager _stageRewardManager;
    private CurrencyManager _currencyManager;
    private GrowthManager _growthManager;
    private GameManager _gameManager;

    private bool _isSubscribed;
    private bool _isSavedForCurrentResult;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    void Start()
    {
        if (Instance != this)
        {
            return;
        }

        CacheManagers();
        SubscribeEvents();
    }

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
    /// 사용할 매니저 참조를 확보
    /// </summary>
    private void CacheManagers()
    {
        _saveManager = SaveManager.Instance;
        _stageManager = StageManager.Instance;
        _stageRewardManager = StageRewardManager.Instance;
        _currencyManager = CurrencyManager.Instance;
        _growthManager = GrowthManager.Instance;
        _gameManager = GameManager.Instance;
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted += HandleStageStarted;
        }

        if (_stageRewardManager != null)
        {
            _stageRewardManager.OnStageRewardSettled += HandleStageRewardSettled;
        }

        if (_growthManager != null)
        {
            _growthManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        _isSubscribed = true;
    }

    /// <summary>
    /// 구독했던 이벤트 해제
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
        }

        if (_stageRewardManager != null)
        {
            _stageRewardManager.OnStageRewardSettled -= HandleStageRewardSettled;
        }

        if (_growthManager != null)
        {
            _growthManager.OnUpgradePurchased -= HandleUpgradePurchased;
        }

        _isSubscribed = false;
    }

    /// <summary>
    /// 새로운 스테이지가 시작되면 다음 결과 화면의 저장을 허용
    /// </summary>
    /// <param name="stageNumber"></param>
    private void HandleStageStarted(int stageNumber)
    {
        _isSavedForCurrentResult = false;
    }

    /// <summary>
    /// 보상 정산이 끝난 뒤 현재 스테이지 결과 데이터를 저장
    /// </summary>
    private void HandleStageRewardSettled(int rewardGold, int totalGold)
    {
        if (_isSavedForCurrentResult)
        {
            return;
        }

        // StageManager가 상태를 StageClear로 변경한 뒤
        // StageRewardManager가 정산 이벤트를 발생시키는 흐름을 확인
        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;
        }

        if (_gameManager == null || _gameManager.State != EGameState.StageClear)
        {
            Debug.LogWarning("결과 화면 상태가 아니므로 자동저장을 건너뜀.");
            return;
        }

        // 중복 이벤트로 같은 결과가 여러번 저장되는 것을 방지
        _isSavedForCurrentResult = true;

        SaveCurrentProgress(totalGold);
    }

    private void HandleUpgradePurchased(string upgradeId, int level, float amount)
    {
        CacheManagers();

        if (_currencyManager == null)
        {
            Debug.LogError("CurrencyManager를 찾을 수 없어 스킬 구매 내역을 저장할 수 없음.");
            return;
        }

        SaveCurrentProgress(_currencyManager.Gold);

        Debug.Log($"스킬 구매 저장 요청: {upgradeId}, 레벨 {level}");
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 현재 진행 데이터를 저장용 DTO로 구성하고 저장을 요청
    /// </summary>
    /// <param name="settledGold">정산 완료된 현재 골드 잔고</param>
    public void SaveCurrentProgress(int settledGold)
    {
        CacheManagers();

        if (_saveManager == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없어 저장할 수 없음.");
            return;
        }

        if (_stageManager == null || _currencyManager == null || _growthManager == null || _gameManager == null)
        {
            Debug.LogError("저장에 필요한 게임 매니저가 준비되지 않음.");
            return;
        }

        int slotIndex = _saveManager.CurrentSlotIndex;

        if (slotIndex < 1 || slotIndex > 3)
        {
            Debug.LogError("저장 슬롯이 지정되지 않음.");
            return;
        }

        // 기존 저장 데이터가 있다면 불러와서 재사용
        SaveSlotData saveData;

        if (_saveManager.HasSaveData(slotIndex))
        {
            saveData = _saveManager.LoadGame(slotIndex);

            // 기존 파일을 읽지 못했다면 새 데이터로 덮으쓰지 않음.
            if (saveData == null)
            {
                Debug.LogError($"슬롯 {slotIndex}의 기존 저장 데이터를 읽지 못해 저장 중단");
                return;
            }
        }
        else
        {
            // 저장 데이터가 없는 새 게임이라면 새 DTO를 생성
            saveData = new SaveSlotData();
            saveData.slotIndex = slotIndex;
        }

        // 현재 플레이어의 영구 재화 정보
        saveData.playerData.legacyPoints = _currencyManager.LegacyPoints;
        if (RoguelikeManager.Instance != null)
        {
            saveData.playerData.bankruptcyCycle = RoguelikeManager.Instance.CycleCount;
        }

        // 현재 런의 진행 정보 데이터
        saveData.runData.isRunActive = _gameManager.IsRunActive;
        saveData.runData.currentStage = _stageManager.CurrentStage;
        saveData.runData.currentGold = settledGold;

        // 스태미나는 저장, 복원 대상으로 사용하지 않음.
        saveData.runData.currentStamina = 0f;

        // 최신 스킬 레벨과 해금 상태를 반영
        saveData.runData.runGrowth = _growthManager.ExportSaveData();

        // 기존 영구 성장 데이터와 구매 아이템 목록은 별도로 초기화하지 않음.
        _saveManager.SaveGame(saveData);

        Debug.Log($"결과 화면 저장 요청: 슬롯 {slotIndex}, " + $"스테이지 {saveData.runData.currentStage}, " + $"보유 골드 {saveData.runData.currentGold}");
    }
    #endregion

}
