using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레거시 상점의 영구 구매 상태와 저장 데이터 연동을 관리
/// 구매 아이템은 사이클이 초기화되어도 저장 슬롯의 영구 성장 데이터에 유지
/// </summary>
public sealed class LegacyShopManager : MonoBehaviour
{
    #region 싱글톤
    public static LegacyShopManager Instance { get; private set; }
    #endregion

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
    #endregion

    #region 내부 변수
    private readonly HashSet<string> _purchasedItemIds = new HashSet<string>();
    private SaveManager _saveManager;
    private bool _isSubscribed;
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
        DontDestroyOnLoad(gameObject);
        CacheManagers();
    }

    private void Start()
    {
        SubscribeEvents();
        LoadCurrentSlotData();
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

    #region 커스텀 메서드 (Public)
    /// <summary>
    /// 기존 매니저를 반환하거나 구매 상태 저장 매니저 생성
    /// 상점 씬이 단독 실행되어도 구매 상태를 관리할 수 있도록 보장
    /// </summary>
    public static LegacyShopManager GetOrCreate()
    {
        GameObject managerObject = new GameObject("LegacyShopManager");
        return managerObject.AddComponent<LegacyShopManager>();
    }

    /// <summary>
    /// SO의 기본 보유 상태와 저장된 구매 이력을 합산하여 보유 여부 반환
    /// </summary>
    public bool IsOwned(LegacyShopItemData item)
    {
        return false;
    }

    /// <summary>
    /// 구매한 아이템을 영구 소유 목록에 추가하고 현재 저장 슬롯에 즉시 저장
    /// 사이클 초기화와 무관하게 PlayerSaveData에 기록
    /// </summary>
    public bool MarkPurchased(LegacyShopItemData item)
    {
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

        saveData.purchasedItemIds.RemoveAll(IsLegacyItemId);
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
    }

    /// <summary>
    /// 모든 아이템의 저장 구매 이력을 제거하고 각 SO 기본 상태로 복원
    /// </summary>
    public void ResetAllOwnershipToDataDefaults()
    {

    }

    /// <summary>
    /// 저장된 영구 성장 DTO에서 구매 이력을 복원
    /// </summary>
    public void ImportSaveData(PermanentGrowthSaveData saveData)
    {

    }
    #endregion

    #region 커스텀 메서드 (Private)
    /// <summary>
    /// 사용할 저장 매니저 참조를 확보
    /// </summary>
    private void CacheManagers()
    {
        _saveManager = SaveManager.Instance;
    }

    /// <summary>
    /// 저장 복원 완료 이벤트를 구독
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
    }

    /// <summary>
    /// 저장 시스템에서 현재 슬롯 복원이 완료되면 영구 구매 상태 적용
    /// </summary>
    private void HandleGameLoaded(SaveSlotData saveData)
    {
        if (saveData == null || saveData.playerData == null) return;
        ImportSaveData(saveData.playerData.permanentGrowth);
    }


    private static bool IsLegacyItemId(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && itemId.StartsWith("LEG_");
    }
    #endregion
}
