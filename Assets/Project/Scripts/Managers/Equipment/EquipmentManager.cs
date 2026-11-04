using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장비 구매 여부와 골드 차감 및 장착 요청을 관리
/// </summary>
public class EquipmentManager : MonoBehaviour
{
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

        _ownedEquipments.Add(data);
    }
    #endregion
}