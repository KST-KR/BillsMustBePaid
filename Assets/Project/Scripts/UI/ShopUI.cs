using TMPro;
using UnityEngine;

/// <summary>
/// 상점에서 선택한 장비의 정보를 표시합니다.
/// </summary>
public class ShopUI : MonoBehaviour
{
    #region 인스펙터
    [Header("상점 입력")]
    [SerializeField] private ShopDisplayInput _shopDisplayInput;

    [Header("플레이어 정보")]
    [SerializeField] private PlayerStats _playerStats;

    [Header("장비 정보 패널")]
    [SerializeField] private GameObject _detailPanel;

    [Header("장비 정보 텍스트")]
    [SerializeField] private TMP_Text _equipmentNameText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("구매 전 능력치")]
    [SerializeField] private TMP_Text _attackPowerBeforeText;
    [SerializeField] private TMP_Text _attackIntervalBeforeText;
    [SerializeField] private TMP_Text _attackRangeBeforeText;

    [Header("구매 후 능력치")]
    [SerializeField] private TMP_Text _attackPowerAfterText;
    [SerializeField] private TMP_Text _attackIntervalAfterText;
    [SerializeField] private TMP_Text _attackRangeAfterText;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if (_shopDisplayInput == null)
        {
            Debug.LogError("ShopUI: ShopDisplayInput이 할당되지 않았습니다.", this);
            return;
        }

        _shopDisplayInput.OnShopItemHovered += HandleShopItemHovered;
        _shopDisplayInput.OnShopItemHoverExited += HandleShopItemHoverExited;

        if (_shopDisplayInput.CurrentShopItem != null)
        {
            UpdateEquipmentInfo(_shopDisplayInput.CurrentShopItem.EquipmentData);
        }
        else
        {
            ClearEquipmentInfo();
        }
    }

    private void OnDisable()
    {
        if (_shopDisplayInput != null)
        {
            _shopDisplayInput.OnShopItemHovered -= HandleShopItemHovered;
            _shopDisplayInput.OnShopItemHoverExited -= HandleShopItemHoverExited;
        }
    }
    #endregion

    #region 이벤트 처리
    private void HandleShopItemHovered(ShopItem shopItem)
    {
        if (shopItem == null)
        {
            ClearEquipmentInfo();
            return;
        }

        UpdateEquipmentInfo(shopItem.EquipmentData);
    }

    private void HandleShopItemHoverExited()
    {
        ClearEquipmentInfo();
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 장비 데이터를 UI에 표시
    /// </summary>
    private void UpdateEquipmentInfo(EquipmentData data)
    {
        if (data == null || _playerStats == null)
        {
            ClearEquipmentInfo();
            return;
        }

        _detailPanel.SetActive(true);

        _equipmentNameText.text = data.EquipmentName;
        _descriptionText.text = data.Description;

        // 구매 전 능력치
        float beforeAttackPower = _playerStats.AttackPower;
        float beforeAttackInterval = _playerStats.AttackInterval;
        float beforeAttackRange = _playerStats.AttackRange;

        // 구매 후 예상 능력치
        float afterAttackPower = _playerStats.GetAttackPowerWithEquipment(data);
        float afterAttackInterval = _playerStats.GetAttackIntervalWithEquipment(data);
        float afterAttackRange = _playerStats.GetAttackRangeWithEquipment(data);

        // 공격력
        _attackPowerBeforeText.text = beforeAttackPower.ToString("F1");
        _attackPowerAfterText.text = afterAttackPower.ToString("F1");

        // 공격 간격
        _attackIntervalBeforeText.text = beforeAttackInterval.ToString("F2");
        _attackIntervalAfterText.text = afterAttackInterval.ToString("F2");

        // 공격 범위
        _attackRangeBeforeText.text = beforeAttackRange.ToString("F1");
        _attackRangeAfterText.text = afterAttackRange.ToString("F1");
    }

    /// <summary>
    /// 장비 정보 초기화
    /// </summary>
    private void ClearEquipmentInfo()
    {
        if (_detailPanel != null)
        {
            _detailPanel.SetActive(false);
        }

        if (_equipmentNameText != null)
        {
            _equipmentNameText.text = string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.Empty;
        }

        if (_attackPowerBeforeText != null)
        {
            _attackPowerBeforeText.text = string.Empty;
        }

        if (_attackPowerAfterText != null)
        {
            _attackPowerAfterText.text = string.Empty;
        }

        if (_attackIntervalBeforeText != null)
        {
            _attackIntervalBeforeText.text = string.Empty;
        }

        if (_attackIntervalAfterText != null)
        {
            _attackIntervalAfterText.text = string.Empty;
        }

        if (_attackRangeBeforeText != null)
        {
            _attackRangeBeforeText.text = string.Empty;
        }

        if (_attackRangeAfterText != null)
        {
            _attackRangeAfterText.text = string.Empty;
        }
    }
    #endregion
}