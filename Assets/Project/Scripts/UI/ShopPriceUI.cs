using TMPro;
using UnityEngine;

/// <summary>
/// 상점에 진열된 장비의 가격과 구매 및 장착 상태를 표시
/// </summary>
public class ShopPriceUI : MonoBehaviour
{
    #region 인스펙터
    [Header("장비 데이터")]
    [SerializeField] private EquipmentDatabase _equipmentDatabase;

    [Header("가격 텍스트")]
    [SerializeField] private TMP_Text[] _priceTexts;

    [Header("장비 관리")]
    [SerializeField] private EquipmentManager _equipmentManager;
    [SerializeField] private PlayerEquipment _playerEquipment;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentPurchased += HandleEquipmentStateChanged;
            _equipmentManager.OnEquipmentEquipped += HandleEquipmentStateChanged;
        }

        UpdateAllPriceTexts();
    }

    private void OnDisable()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentPurchased -= HandleEquipmentStateChanged;
            _equipmentManager.OnEquipmentEquipped -= HandleEquipmentStateChanged;
        }
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 장비 구매 및 장착 이벤트 처리
    /// </summary>
    /// <param name="data"></param>
    private void HandleEquipmentStateChanged(EquipmentData data)
    {
        UpdateAllPriceTexts();
    }

    /// <summary>
    /// 모든 장비의 가격 및 상태 표시 갱신
    /// </summary>
    private void UpdateAllPriceTexts()
    {
        if (_equipmentManager == null || _priceTexts == null || _equipmentManager == null || _playerEquipment == null)
        {
            Debug.LogWarning("ShopPriceUI의 참조가 올바르게 연결되지 않음.", this);
            return;
        }

        var equipments = _equipmentDatabase.Equipments;

        if (_priceTexts.Length != equipments.Count)
        {
            Debug.LogWarning("장비 데이터 개수와 가격 텍스트 개수가 일치하지 않음.", this);
        }

        int count = Mathf.Min(_priceTexts.Length, equipments.Count);

        for (int i = 0; i < count; i++)
        {
            TMP_Text prictText = _priceTexts[i];
            EquipmentData data = equipments[i];

            if (prictText == null || data == null)
            {
                continue;
            }

            UpdatePriceText(prictText, data);
        }
    }

    private void UpdatePriceText(TMP_Text prictText, EquipmentData data)
    {
        if (_playerEquipment.CurrentEquipment == data)
        {
            prictText.text = "장착됨";
        }
        else if (_equipmentManager.IsPurchased(data))
        {
            prictText.text = "구매됨";
        }
        else if (_equipmentManager.IsOwned(data))
        {
            prictText.text = "보유됨";
        }
        else
        {
            prictText.text = $"${data.Price}";
        }
    }
    #endregion
}
