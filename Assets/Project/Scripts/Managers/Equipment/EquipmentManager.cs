using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장비 구매 여부와 골드 차감 및 장착 요청을 관리
/// </summary>
public class EquipmentManager : Singleton<EquipmentManager>
{
    #region 인스펙터
    [Header("장비 데이터베이스")]
    [SerializeField] private EquipmentDatabase _equipmentDatabase;
    #endregion

    #region 이벤트
    // 장비 구매가 완료되었을 때 발생
    public event Action<EquipmentData> OnEquipmentPurchased;

    // 장비 장착이 완료되었을 때 발생
    public event Action<EquipmentData> OnEquipmentEquipped;
    #endregion

    #region 내부 변수
    // 이번 런에서 보유한 장비 목록
    private HashSet<EquipmentData> _ownedEquipments = new HashSet<EquipmentData>();

    // 이번 런에서 구매한 장비 목록
    private HashSet<EquipmentData> _purchasedEquipments = new HashSet<EquipmentData>();

    // 게임 시작 시 기본으로 지급되는 장비
    private HashSet<EquipmentData> _startingEquipments = new HashSet<EquipmentData>();

    private SaveManager _saveManager;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        _saveManager = SaveManager.Instance;

        if (_saveManager != null)
        {
            _saveManager.OnGameLoaded += HandleGameLoaded;
        }
    }

    private void OnDisable()
    {
        if (_saveManager != null)
        {
            _saveManager.OnGameLoaded -= HandleGameLoaded;
            _saveManager = null;
        }
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 저장된 장비 보유 및 구매 데이터를 복원
    /// </summary>
    private void HandleGameLoaded(SaveSlotData saveData)
    {
        if (_equipmentDatabase == null)
        {
            Debug.LogError("EquipmentDatabase가 연결되지 않음.");
            return;
        }

        if (saveData == null || saveData.runData == null)
        {
            Debug.LogWarning("장비 복원에 필요한 저장 데이터가 없음.");
            return;
        }

        EquipmentSaveData equipmentSaveData = saveData.runData.equipmentData;

        // 1. 보유 및 구매 목록 복원
        RestoreSaveData(equipmentSaveData, _equipmentDatabase);

        if (equipmentSaveData == null)
        {
            return;
        }

        // 2. 장착 장비 ID 확인
        if (string.IsNullOrWhiteSpace(equipmentSaveData.equippedEquipmentId))
        {
            Debug.LogWarning("저장된 장착 장비 ID가 없음.");
            return;
        }

        // 3. ID를 실제 장비 데이터로 변환
        EquipmentData equipment = _equipmentDatabase.FindEquipmentById(equipmentSaveData.equippedEquipmentId);

        if (equipment == null)
        {
            Debug.LogWarning($"저장된 장착 장비 데이터를 찾을 수 없음: {equipmentSaveData.equippedEquipmentId}");
            return;
        }

        // 4. PlayerEquipment에 장착 요청
        OnEquipmentEquipped?.Invoke(equipment);

        Debug.Log($"{equipment.EquipmentName}: 저장된 장비 복원 요청 완료");
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 해당 장비를 보유하고 있는지 확인
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool IsOwned(EquipmentData data)
    {
        return data != null && _ownedEquipments.Contains(data);
    }

    /// <summary>
    /// 해당 장비를 이미 구매했는지 확인
    /// </summary>
    public bool IsPurchased(EquipmentData data)
    {
        return data != null && _purchasedEquipments.Contains(data);
    }

    /// <summary>
    /// 장비를 구매하거나 이미 구매한 장비를 재장착
    /// </summary>
    public bool TryPurchaseOrEquip(EquipmentData data)
    {
        if (data == null)
        {
            Debug.LogWarning("구매할 장비 데이터가 없음.");
            return false;
        }

        // 이미 구매한 장비라면 골드 차감 없이 재장착
        if (IsOwned(data))
        {
            Debug.Log($"{data.EquipmentName}: 구매한 장비 재장착");
            OnEquipmentEquipped?.Invoke(data);
            return true;
        }

        // 구매 가격 확인
        if (data.Price <= 0)
        {
            Debug.LogWarning($"{data.EquipmentName}: 장비 가격이 올바르지 않음.");
            return false;
        }

        // 재화 관리자 확인
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager를 찾을 수 없음.");
            return false;
        }

        // 골드 차감 시도
        bool isPurchased = CurrencyManager.Instance.TrySpend(ECurrencyType.Gold, data.Price, ECurrencyTransactionType.Purchase);

        if (!isPurchased)
        {
            Debug.Log("골드가 부족하여 장비를 구매할 수 없음.");
            return false;
        }

        // 구매 목록에 등록
        _ownedEquipments.Add(data);
        _purchasedEquipments.Add(data);

        Debug.Log($"{data.EquipmentName}: 구매 성공!");

        // 구매 완료 이벤트
        OnEquipmentPurchased?.Invoke(data);

        // 장착 이벤트
        OnEquipmentEquipped?.Invoke(data);

        return true;
    }

    public void RegisterStartingEquipment(EquipmentData data)
    {
        if (data == null)
        {
            Debug.LogWarning("등록할 기본 장비 데이터가 없음.");
            return;
        }

        _startingEquipments.Add(data);
        _ownedEquipments.Add(data);
    }

    /// <summary>
    /// 현재 장비 상태를 저장 데이터로 변환
    /// </summary>
    /// <param name="equippedEquipment"></param>
    /// <returns></returns>
    public EquipmentSaveData CreateSaveData(EquipmentData equippedEquipment)
    {
        EquipmentSaveData saveData = new EquipmentSaveData();

        // 현재 장착 장비 ID 저장
        if (equippedEquipment != null)
        {
            saveData.equippedEquipmentId = equippedEquipment.EquipmentId;
        }

        // 보유 장비 목록 저장
        EquipmentData[] ownedEquipments = new EquipmentData[_ownedEquipments.Count];
        _ownedEquipments.CopyTo(ownedEquipments);

        for (int i = 0; i < ownedEquipments.Length; i++)
        {
            EquipmentData equipment = ownedEquipments[i];

            if (equipment == null)
            {
                continue;
            }

            saveData.ownedEquipmentIds.Add(equipment.EquipmentId);
        }

        // 구매 장비 목록 저장
        EquipmentData[] purchasedEquipments = new EquipmentData[_purchasedEquipments.Count];
        _purchasedEquipments.CopyTo(purchasedEquipments);

        for (int i = 0; i < purchasedEquipments.Length; i++)
        {
            EquipmentData equipment = purchasedEquipments[i];

            if (equipment == null)
            {
                continue;
            }

            saveData.purchasedEquipmentIds.Add(equipment.EquipmentId);
        }

        return saveData;
    }

    /// <summary>
    /// 저장된 장비 데이터로 보유 및 구매 상태를 복원
    /// </summary>
    /// <param name="saveData"></param>
    /// <param name="database"></param>
    public void RestoreSaveData(EquipmentSaveData saveData, EquipmentDatabase database)
    {
        if (saveData == null || database == null)
        {
            Debug.LogWarning("장비 저장 데이터를 복원할 수 없음.");
            return;
        }

        if (saveData.ownedEquipmentIds == null || saveData.purchasedEquipmentIds == null)
        {
            Debug.LogWarning("장비 저장 목록이 초기화되지 않음.");
            return;
        }

        // 기존 보유 및 구매 목록 초기화
        _ownedEquipments.Clear();
        _purchasedEquipments.Clear();

        // 기본 장비는 항상 보유 상태로 유지
        EquipmentData[] startingEquipments = new EquipmentData[_startingEquipments.Count];

        _startingEquipments.CopyTo(startingEquipments);

        for (int i = 0; i< startingEquipments.Length; i++)
        {
            if (startingEquipments[i] != null)
            {
                _ownedEquipments.Add(startingEquipments[i]);
            }
        }

        // 보유 장비 복원
        for (int i = 0; i < saveData.ownedEquipmentIds.Count; i++)
        {
            string equipmentId = saveData.ownedEquipmentIds[i];

            EquipmentData equipment = database.FindEquipmentById(equipmentId);

            if (equipment == null)
            {
                Debug.LogWarning($"보유 장비 데이터를 찾을 수 없음: {equipmentId}");
                continue;
            }

            _ownedEquipments.Add(equipment);
        }

        // 구매 장비 복원
        for (int i = 0; i < saveData.purchasedEquipmentIds.Count; i++)
        {
            string equipmentId = saveData.purchasedEquipmentIds[i];

            EquipmentData equipment = database.FindEquipmentById(equipmentId);

            if (equipment == null)
            {
                Debug.LogWarning($"구매 장비 데이터를 찾을 수 없음: {equipmentId}");
                continue;
            }

            _purchasedEquipments.Add(equipment);

            // 구매한 장비는 반드시 보유 상태에도 포함
            _ownedEquipments.Add(equipment);
        }
    }
    #endregion
}