using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 구매 아이템은 사이클이 초기화되어도 저장 슬롯의 영구 성장 데이터에 유지
/// </summary>
public sealed class LegacyShopManager : PersistentSingleton<LegacyShopManager>
{
    #region 속성
    public IReadOnlyCollection<string> PurchasedItemIds => _purchasedItemIds;
    #endregion

    #region 이벤트
    /// <summary>
    /// 구매 상태 복원 또는 변경 시 상점 UI 구독자에게 알림
    /// </summary>
    public event Action<string> OnOwnershipChanged;
    public event Action OnOwnershipDataChanged;
    #endregion

    #region 인스펙터
    [SerializeField] private LegacyShopItemData[] _items = new LegacyShopItemData[0];
    [SerializeField] private Button _refundAndResetButton;
    [SerializeField] private Button _startNextCycleButton;
    #endregion

    #region 내부 변수
    private readonly HashSet<string> _purchasedItemIds = new HashSet<string>();
    private SaveManager _saveManager;
    private CurrencyManager _currencyManager;
    private bool _isSubscribed;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        CacheManagers();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        SubscribeEvents();
        BindShopButtons();
        LoadCurrentSlotData();
        UpdateNextCycleButtonLabel();
    }

    protected override void OnSingletonDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        UnbindShopButtons();
        UnsubscribeEvents();

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged -= HandleBalanceChanged;
            _currencyManager = null;
        }
    }
    #endregion

    #region 커스텀 메서드 (Public)
    /// <summary>
    /// 기존 매니저를 반환하거나 구매 상태 저장 매니저 생성
    /// 상점 씬이 단독 실행되어도 구매 상태를 관리할 수 있도록 보장
    /// </summary>
    public static LegacyShopManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LegacyShopManager existing = FindObjectOfType<LegacyShopManager>();

        if (existing != null)
        {
            return existing;
        }

        GameObject managerObject = new GameObject("LegacyShopManager");
        return managerObject.AddComponent<LegacyShopManager>();
    }

    /// <summary>
    /// SO의 기본 보유 상태와 현재 저장 슬롯의 구매 이력을 합산하여 보유 여부 반환
    /// </summary>
    public bool IsOwned(LegacyShopItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.ItemId))
        {
            return false;
        }

        return item.InitiallyOwned || _purchasedItemIds.Contains(item.ItemId);
    }

    /// <summary>
    /// 구매한 아이템을 영구 소유 목록에 추가하고 현재 저장 슬롯에 즉시 저장
    /// 사이클 초기화와 무관하게 PlayerSaveData에 기록
    /// </summary>
    public bool TryPurchase(LegacyShopItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.ItemId) || IsOwned(item) ||
            CurrencyManager.Instance == null ||
            !CurrencyManager.Instance.TrySpend(ECurrencyType.LegacyPoint, item.Price,
                ECurrencyTransactionType.Purchase))
        {
            return false;
        }

        _purchasedItemIds.Add(item.ItemId);
        SaveCurrentSlotData();
        OnOwnershipChanged?.Invoke(item.ItemId);
        OnOwnershipDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// [FIN-03-03] 구매한 모든 반지를 기본 상태로 초기화하고 사용 포인트를 전액 환불
    /// </summary>
    public int RefundAndResetAllPurchases()
    {
        CacheManagers();
        int refundAmount = 0;
        foreach (string itemId in _purchasedItemIds)
        {
            LegacyShopItemData item = FindItem(itemId);
            if (item == null)
            {
                Debug.LogError($"Item Not Found: {itemId}");
                return 0;
            }

            long nextRefundAmount = (long)refundAmount + item.Price;
            if (nextRefundAmount > int.MaxValue)
            {
                Debug.LogWarning("환불 포인트 최대치 초과");
                nextRefundAmount = int.MaxValue;                
            }
            refundAmount = (int)nextRefundAmount;
        }

        if (refundAmount > 0 && (_currencyManager == null ||
            !_currencyManager.TryAdd(ECurrencyType.LegacyPoint, refundAmount)))
        {
            Debug.LogError("레거시 포인트 환불에 실패");
            return 0;
        }

        List<string> purchasedItemIds = new List<string>(_purchasedItemIds);
        _purchasedItemIds.Clear();
        SaveCurrentSlotData();
        OnOwnershipDataChanged?.Invoke();
        Debug.Log("구매한 레거시 아이템 " + purchasedItemIds.Count + "개 초기화, " + refundAmount + " 포인트 환불");
        return refundAmount;
    }

    /// <summary>
    /// [FIN-03-03] 현재 사이클 진행 상태를 저장한 뒤 Game 씬에서 다음 런 시작을 요청
    /// </summary>
    public void StartNextCycle()
    {
        CacheManagers();
        if (_saveManager == null || _currencyManager == null || _saveManager.CurrentSlotIndex < 1 ||
            _saveManager.CurrentSlotIndex > 3 || SceneTransitionManager.Instance == null)
        {
            Debug.LogError("매니저 누락 확인");
            return;
        }

        SaveCurrentSlotData();
        SaveSlotData savedData = _saveManager.LoadGame(_saveManager.CurrentSlotIndex);
        if (savedData == null || savedData.playerData == null ||
            savedData.playerData.legacyPoints != _currencyManager.LegacyPoints ||
            !HasAllPurchasedItems(savedData.playerData.permanentGrowth))
        {
            Debug.LogError("레거시 포인트 저장 오류");
            return;
        }

        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.StartNextCycle, _saveManager.CurrentSlotIndex);
        SceneTransitionManager.Instance.LoadGameScene();
    }

    private bool HasAllPurchasedItems(PermanentGrowthSaveData saveData)
    {
        if (saveData == null || saveData.purchasedItemIds == null) return _purchasedItemIds.Count == 0;
        foreach (string itemId in _purchasedItemIds)
        {
            if (!saveData.purchasedItemIds.Contains(itemId)) return false;
        }
        return true;
    }

    /// <summary>
    /// 저장 데이터에 레거시 구매 목록을 반영하며 다른 영구 아이템 ID는 보존
    /// </summary>
    public PermanentGrowthSaveData ExportSaveData(PermanentGrowthSaveData saveData)
    {
        if (saveData == null)
        {
            saveData = new PermanentGrowthSaveData();
        }
        if (saveData.purchasedItemIds == null)
        {
            saveData.purchasedItemIds = new List<string>();
        }

        saveData.purchasedItemIds.RemoveAll(IsConfiguredItemId);
        foreach (string itemId in _purchasedItemIds)
        {
            if (!saveData.purchasedItemIds.Contains(itemId))
            {
                saveData.purchasedItemIds.Add(itemId);
            }
        }
        return saveData;
    }

    /// <summary>
    /// 선택한 아이템의 저장 구매 이력을 제거하고 SO 기본 상태로 복원
    /// </summary>
    public void ResetOwnershipToDataDefault(LegacyShopItemData item)
    {
        if (item == null) return;

        _purchasedItemIds.Remove(item.ItemId);
        SaveCurrentSlotData();
        OnOwnershipChanged?.Invoke(item.ItemId);
        OnOwnershipDataChanged?.Invoke();
    }

    /// <summary>
    /// 모든 아이템의 저장 구매 이력을 제거하고 각 SO 기본 상태로 복원
    /// </summary>
    public void ResetAllOwnershipToDataDefaults()
    {
        _purchasedItemIds.Clear();
        SaveCurrentSlotData();
        OnOwnershipDataChanged?.Invoke();
    }

    /// <summary>
    /// 저장된 영구 성장 DTO에서 구매 이력을 복원
    /// </summary>
    public void ImportSaveData(PermanentGrowthSaveData saveData)
    {
        _purchasedItemIds.Clear();
        if (saveData != null && saveData.purchasedItemIds != null)
        {
            for (int i = 0; i < saveData.purchasedItemIds.Count; i++)
            {
                string itemId = saveData.purchasedItemIds[i];
                if (IsConfiguredItemId(itemId))
                {
                    _purchasedItemIds.Add(itemId);
                }
            }
        }
        OnOwnershipDataChanged?.Invoke();
    }

    /// <summary>
    /// [FIN-03-03] 현재 영구 보유한 레거시 반지의 지정 능력치 보너스 합산
    /// </summary>
    public float GetStatBonus(EUpgradeStat stat)
    {
        float totalBonus = 0.0f;
        for (int i = 0; i < _items.Length; i++)
        {
            LegacyShopItemData item = _items[i];
            if (item == null || item.TargetStat != stat ||
                (!item.InitiallyOwned && !_purchasedItemIds.Contains(item.ItemId)))
            {
                continue;
            }

            totalBonus += item.StatValue;
        }
        return totalBonus;
    }

    /// <summary>
    /// 현재 저장 슬롯에서 보유 중인 레거시 아이템 데이터를 반환
    /// 처음부터 보유한 아이템도 장착 아이템에 포함
    /// </summary>
    public List<LegacyShopItemData> GetOwnedItems()
    {
        List<LegacyShopItemData> ownedItems = new List<LegacyShopItemData>();
        for (int i = 0; i < _items.Length; i++)
        {
            LegacyShopItemData item = _items[i];
            if (item != null && (item.InitiallyOwned || _purchasedItemIds.Contains(item.ItemId)))
            {
                ownedItems.Add(item);
            }
        }

        return ownedItems;
    }
    #endregion

    #region 커스텀 메서드 (Private)
    /// <summary>
    /// 상점 씬 재진입 시 새 씬의 버튼과 저장 구매 정보를 다시 연결
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameLegacyShop") return;

        _refundAndResetButton = null;
        _startNextCycleButton = null;
        BindShopButtons();
        LoadCurrentSlotData();
        UpdateNextCycleButtonLabel();
    }

    private void BindShopButtons()
    {
        if (_refundAndResetButton == null)
        {
            GameObject buttonObject = GameObject.Find("Reset Points");
            if (buttonObject != null) _refundAndResetButton = buttonObject.GetComponent<Button>();
        }
        if (_startNextCycleButton == null)
        {
            GameObject buttonObject = GameObject.Find("Start Cycle");
            if (buttonObject != null) _startNextCycleButton = buttonObject.GetComponent<Button>();
        }

        if (_refundAndResetButton != null)
        {
            _refundAndResetButton.onClick.RemoveListener(HandleRefundAndResetButtonClicked);
            _refundAndResetButton.onClick.AddListener(HandleRefundAndResetButtonClicked);
        }
        if (_startNextCycleButton != null)
        {
            _startNextCycleButton.onClick.RemoveListener(StartNextCycle);
            _startNextCycleButton.onClick.AddListener(StartNextCycle);
        }
    }

    private void UnbindShopButtons()
    {
        if (_refundAndResetButton != null)
        {
            _refundAndResetButton.onClick.RemoveListener(HandleRefundAndResetButtonClicked);
        }
        if (_startNextCycleButton != null)
        {
            _startNextCycleButton.onClick.RemoveListener(StartNextCycle);
        }
    }

    /// <summary>
    /// 환불 결과를 반환하는 메서드를 Unity Button 이벤트 형식으로 연결
    /// </summary>
    private void HandleRefundAndResetButtonClicked()
    {
        RefundAndResetAllPurchases();
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        if (type == ECurrencyType.LegacyPoint)
        {
            SaveCurrentSlotData();
        }
    }

    /// <summary>
    /// 사용할 저장 매니저 참조를 확보
    /// </summary>
    private void CacheManagers()
    {
        _saveManager = SaveManager.Instance;
        _currencyManager = CurrencyManager.Instance;
    }

    /// <summary>
    /// 저장 복원 완료 이벤트와 레거시 포인트 잔액 변경을 구독
    /// </summary>
    private void SubscribeEvents()
    {
        if (_isSubscribed) return;
        CacheManagers();
        if (_saveManager != null)
        {
            _saveManager.OnGameLoaded += HandleGameLoaded;
            _isSubscribed = true;
        }
        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged -= HandleBalanceChanged;
            _currencyManager.OnBalanceChanged += HandleBalanceChanged;
        }
    }

    /// <summary>
    /// 저장 복원 완료 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed || _saveManager == null) return;
        _saveManager.OnGameLoaded -= HandleGameLoaded;
        _isSubscribed = false;
    }

    /// <summary>
    /// 현재 선택된 슬롯의 구매 정보를 초기 구동 시 복원
    /// </summary>
    private void LoadCurrentSlotData()
    {
        CacheManagers();
        if (_saveManager == null || _saveManager.CurrentSlotIndex < 1 || _saveManager.CurrentSlotIndex > 3 ||
            !_saveManager.HasSaveData(_saveManager.CurrentSlotIndex))
        {
            OnOwnershipDataChanged?.Invoke();
            return;
        }

        SaveSlotData saveData = _saveManager.LoadGame(_saveManager.CurrentSlotIndex);
        if (saveData != null && saveData.playerData != null)
        {
            ImportSaveData(saveData.playerData.permanentGrowth);
        }
    }

    /// <summary>
    /// 저장 시스템에서 현재 슬롯 복원이 완료되면 영구 구매 상태 적용
    /// </summary>
    private void HandleGameLoaded(SaveSlotData saveData)
    {
        if (saveData == null || saveData.playerData == null) return;
        ImportSaveData(saveData.playerData.permanentGrowth);
        UpdateNextCycleButtonLabel(saveData.playerData.bankruptcyCycle);
    }

    private void UpdateNextCycleButtonLabel()
    {
        if (_saveManager == null || _saveManager.CurrentSlotIndex < 1) return;
        SaveSlotData saveData = _saveManager.LoadGame(_saveManager.CurrentSlotIndex);
        if (saveData != null && saveData.playerData != null)
        {
            UpdateNextCycleButtonLabel(saveData.playerData.bankruptcyCycle);
        }
    }

    private void UpdateNextCycleButtonLabel(int cycle)
    {
        if (_startNextCycleButton == null) return;
        Transform labelTransform = _startNextCycleButton.transform.Find("Button Label");
        TMP_Text label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        if (label != null)
        {
            label.text = "사이클 " + Mathf.Max(1, cycle) + " 시작";
        }
    }

    /// <summary>
    /// 구매 즉시 현재 저장 슬롯의 영구 성장 목록에 반영
    /// </summary>
    private void SaveCurrentSlotData()
    {
        CacheManagers();
        if (_saveManager == null || _saveManager.CurrentSlotIndex < 1 || _saveManager.CurrentSlotIndex > 3)
        {
            return;
        }

        int slotIndex = _saveManager.CurrentSlotIndex;
        SaveSlotData saveData = _saveManager.HasSaveData(slotIndex) ? _saveManager.LoadGame(slotIndex) : null;
        if (saveData == null)
        {
            if (_saveManager.HasSaveData(slotIndex))
            {
                Debug.LogError("기존 저장 데이터 로드 오류ㅠ");
                return;
            }

            saveData = new SaveSlotData();
            saveData.slotIndex = slotIndex;
        }

        if (saveData.playerData == null) saveData.playerData = new PlayerSaveData();
        if (CurrencyManager.Instance != null)
        {
            saveData.playerData.legacyPoints = CurrencyManager.Instance.LegacyPoints;
        }
        saveData.playerData.permanentGrowth = ExportSaveData(saveData.playerData.permanentGrowth);
        _saveManager.SaveGame(saveData);
    }

    private LegacyShopItemData FindItem(string itemId)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null && _items[i].ItemId == itemId)
            {
                return _items[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 현재 상점에 설정된 아이템 ID인지 확인
    /// </summary>
    private bool IsConfiguredItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null && _items[i].ItemId == itemId) return true;
        }
        return false;
    }
    #endregion
}
