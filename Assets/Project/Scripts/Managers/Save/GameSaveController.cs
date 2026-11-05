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
    private EquipmentManager _equipmentManager;
    private PlayerEquipment _playerEquipment;

    private bool _isSubscribed;
    private bool _isSavedForCurrentResult;
    private bool _isSkillTreeOnlyMode;
    private bool _savedStageInProgress;
    private bool _isRestoringSaveData;
    #endregion

    #region 프로퍼티
    public bool IsSkillTreeOnlyMode => _isSkillTreeOnlyMode;
    public bool SavedStageInProgress => _savedStageInProgress;
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
        _equipmentManager = EquipmentManager.Instance;

        if (_playerEquipment == null)
        {
            _playerEquipment = FindObjectOfType<PlayerEquipment>();
        }
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

        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentEquipped += HandleEquipmentEquipped;
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

        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentEquipped -= HandleEquipmentEquipped;
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

        // 실제 스테이지가 시작되었으므로 스킬트리 전용 모드 해제
        _isSkillTreeOnlyMode = false;
        _savedStageInProgress = false;

        CacheManagers();

        if (_currencyManager == null)
        {
            Debug.LogError("CurrencyManager를 찾을 수 없어 스테이지 시작 데이터를 저장할 수 없음.");
            return;
        }

        if (SaveCurrentProgress(_currencyManager.Gold))
        {
            Debug.Log($"스테이지 시작 데이터 저장 완료: 스테이지 {stageNumber}");
        }
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

        // 저장에 성공한 경우에만 중복 저장 방지 플래그 설정
        if (SaveCurrentProgress(totalGold))
        {
            _isSavedForCurrentResult = true;
        }
    }

    private void HandleUpgradePurchased(string upgradeId, int level, float amount)
    {
        if (_isRestoringSaveData)
        {
            Debug.Log("저장 데이터 복원 중이므로 자동 저장을 건너뜀.");
            return;
        }

        CacheManagers();

        if (_currencyManager == null)
        {
            Debug.LogError("CurrencyManager를 찾을 수 없어 스킬 구매 내역을 저장할 수 없음.");
            return;
        }

        SaveCurrentProgress(_currencyManager.Gold);

        Debug.Log($"스킬 구매 저장 요청: {upgradeId}, 레벨 {level}");
    }


    /// <summary>
    /// 장비 구매 및 장착 이후 현재 진행 상황 저장
    /// </summary>
    private void HandleEquipmentEquipped(EquipmentData equipment)
    {
        if (_isRestoringSaveData)
        {
            Debug.Log("저장 데이터 복원 중이므로 자동 저장을 건너뜀.");
            return;
        }

        if (equipment == null)
        {
            return;
        }

        CacheManagers();

        if (_currencyManager == null)
        {
            Debug.LogWarning("[GameSaveController] CurrencyManager를 찾을 수 없음.");
            return;
        }

        SaveCurrentProgress(_currencyManager.Gold, equipment);
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 현재 진행 데이터를 저장용 DTO로 구성하고 저장을 요청
    /// </summary>
    /// <param name="settledGold">정산 완료된 현재 골드 잔고</param>
    public bool SaveCurrentProgress(int settledGold, EquipmentData equippedEquipment = null)
    {
        CacheManagers();

        if (_saveManager == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없어 저장할 수 없음.");
            return false;
        }

        if (_stageManager == null || _currencyManager == null ||
            _growthManager == null || _gameManager == null)
        {
            Debug.LogError("저장에 필요한 게임 매니저가 준비되지 않음.");
            return false;
        }

        int slotIndex = _saveManager.CurrentSlotIndex;

        if (slotIndex < 1 || slotIndex > 3)
        {
            Debug.LogError("저장 슬롯이 지정되지 않음.");
            return false;
        }

        SaveSlotData saveData;

        if (_saveManager.HasSaveData(slotIndex))
        {
            saveData = _saveManager.LoadGame(slotIndex);

            if (saveData == null)
            {
                Debug.LogError($"슬롯 {slotIndex}의 기존 저장 데이터를 읽지 못해 저장 중단");
                return false;
            }
        }
        else
        {
            saveData = new SaveSlotData();
            saveData.slotIndex = slotIndex;
        }

        // 현재 플레이어의 영구 재화 정보
        saveData.playerData.legacyPoints = _currencyManager.LegacyPoints;

        if (RoguelikeManager.Instance != null)
        {
            saveData.playerData.bankruptcyCycle = RoguelikeManager.Instance.CycleCount;
        }

        // 현재 런의 진행 정보
        saveData.runData.isRunActive = _gameManager.IsRunActive;
        saveData.runData.currentStage = _stageManager.CurrentStage;

        if (_isSkillTreeOnlyMode)
        {
            saveData.runData.isStageInProgress = _savedStageInProgress;
        }
        else
        {
            saveData.runData.isStageInProgress = _stageManager.IsStageActive;
        }

        saveData.runData.currentGold = settledGold;
        saveData.runData.currentStamina = 0f;

        saveData.runData.runGrowth = _growthManager.ExportSaveData();

        // 장비 데이터 저장
        if (_equipmentManager == null || _playerEquipment == null)
        {
            Debug.LogError("장비 저장에 필요한 참조가 없음.");
            return false;
        }

        EquipmentData equipmentToSave =
            equippedEquipment != null
                ? equippedEquipment
                : _playerEquipment.CurrentEquipment;

        saveData.runData.equipmentData =
            _equipmentManager.CreateSaveData(equipmentToSave);

        // 실제 파일 저장
        bool isSaved = _saveManager.SaveGame(saveData);

        if (!isSaved)
        {
            Debug.LogError("현재 진행 데이터 저장에 실패.");
            return false;
        }

        Debug.Log(
            $"진행 데이터 저장 완료: 슬롯 {slotIndex}, " + $"런 활성화 여부 {saveData.runData.isRunActive}, " + $"스테이지 {saveData.runData.currentStage}, " + $"스테이지 진행 여부 {saveData.runData.isStageInProgress}, " + $"보유 골드 {saveData.runData.currentGold}");

        return true;
    }

    /// <summary>
    /// 스킬트리 전용 진입 상태를 설정
    /// </summary>
    /// <param name="isStageInProgress"></param>
    public void SetSkillTreeOnlyMode(bool isStageInProgress)
    {
        _isSkillTreeOnlyMode = true;
        _savedStageInProgress = isStageInProgress;

        Debug.Log($"스킬트리 전용 진입 설정: 기존 스테이지 진행 여부 {_savedStageInProgress}");
    }

    /// <summary>
    /// 저장 데이터 복원 중인지 설정
    /// </summary>
    public void SetRestoringSaveData(bool isRestoring)
    {
        _isRestoringSaveData = isRestoring;
    }

    #endregion

}
