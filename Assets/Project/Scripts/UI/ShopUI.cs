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
    [SerializeField] private TMP_Text _criticalChanceBeforeText;
    [SerializeField] private TMP_Text _criticalDamageBeforeText;
    [SerializeField] private TMP_Text _staminaCostBeforeText;
    [SerializeField] private TMP_Text _attackRangeBeforeText;

    [Header("구매 후 능력치")]
    [SerializeField] private TMP_Text _attackPowerAfterText;
    [SerializeField] private TMP_Text _attackIntervalAfterText;
    [SerializeField] private TMP_Text _criticalChanceAfterText;
    [SerializeField] private TMP_Text _criticalDamageAfterText;
    [SerializeField] private TMP_Text _staminaCostAfterText;
    [SerializeField] private TMP_Text _attackRangeAfterText;

    [Header("장비 관리")]
    [SerializeField] private EquipmentManager _equipmentManager;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        // 장비 장착 이벤트 구독
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentEquipped += HandleEquipmentEquipped;
        }

        // 상점 입력 이벤트 구독
        if (_shopDisplayInput == null)
        {
            Debug.LogError("ShopUI: ShopDisplayInput이 할당되지 않았습니다.", this);
            ClearEquipmentInfo();
            return;
        }

        _shopDisplayInput.OnShopItemHovered += HandleShopItemHovered;
        _shopDisplayInput.OnShopItemHoverExited += HandleShopItemHoverExited;
        _shopDisplayInput.OnShopItemClicked += HandleShopItemClicked;

        // 현재 마우스가 가리키는 장비 정보 표시
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
            _shopDisplayInput.OnShopItemClicked -= HandleShopItemClicked;
        }

        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentEquipped -= HandleEquipmentEquipped;
        }
    }
    #endregion

    #region 커스텀 (private)
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

    /// <summary>
    /// 장비 장착 이벤트 처리
    /// </summary>
    private void HandleEquipmentEquipped(EquipmentData data)
    {
        // 현재 마우스가 가리키는 장비 정보 갱신
        if (_shopDisplayInput != null && _shopDisplayInput.CurrentShopItem != null)
        {
            UpdateEquipmentInfo(_shopDisplayInput.CurrentShopItem.EquipmentData);
        }
    }

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
        float beforeAttackSpeed = 1f / _playerStats.AttackInterval;
        float beforeCriticalChance = _playerStats.CriticalChance;
        float beforeCriticalDamage = _playerStats.CriticalDamageMultiplier;
        float beforeStaminaCost = _playerStats.StaminaDrainMultiplier;
        float beforeAttackRange = _playerStats.AttackRange;

        // 구매 후 예상 능력치
        float afterAttackPower = _playerStats.GetAttackPowerWithEquipment(data);
        float afterAttackInterval = _playerStats.GetAttackIntervalWithEquipment(data);
        float afterAttackSpeed = 1f / afterAttackInterval;
        float afterCriticalChance = _playerStats.GetCriticalChanceWithEquipment(data);
        float afterCriticalDamage = _playerStats.GetCriticalDamageMultiplierWithEquipment(data);
        float afterStaminaCost = _playerStats.GetStaminaDrainMultiplierWithEquipment(data);
        float afterAttackRange = _playerStats.GetAttackRangeWithEquipment(data);

        // 공격력
        _attackPowerBeforeText.text = beforeAttackPower.ToString("F1");
        _attackPowerAfterText.text = afterAttackPower.ToString("F1");

        // 공격 속도
        _attackIntervalBeforeText.text = $"{beforeAttackSpeed:F1}회/초";
        _attackIntervalAfterText.text = $"{afterAttackSpeed:F1}회/초";

        // 치명타 확률
        _criticalChanceBeforeText.text = $"{beforeCriticalChance * 100f:F1}%";
        _criticalChanceAfterText.text = $"{afterCriticalChance * 100f:F1}%";

        // 치명타 피해 배율
        _criticalDamageBeforeText.text = $"{beforeCriticalDamage:F1}배";
        _criticalDamageAfterText.text = $"{afterCriticalDamage:F1}배";

        // 스태미나 소모 배율
        _staminaCostBeforeText.text = $"{beforeStaminaCost:F1}배";
        _staminaCostAfterText.text = $"{afterStaminaCost:F1}배";

        // 타격 판정 범위
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

        if (_criticalChanceBeforeText != null)
        {
            _criticalChanceBeforeText.text = string.Empty;
        }

        if (_criticalChanceAfterText != null)
        {
            _criticalChanceAfterText.text = string.Empty;
        }

        if (_criticalDamageBeforeText != null)
        {
            _criticalDamageBeforeText.text = string.Empty;
        }

        if (_criticalDamageAfterText != null)
        {
            _criticalDamageAfterText.text = string.Empty;
        }

        if (_staminaCostBeforeText != null)
        {
            _staminaCostBeforeText.text = string.Empty;
        }

        if (_staminaCostAfterText != null)
        {
            _staminaCostAfterText.text = string.Empty;
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

    /// <summary>
    /// 선택한 장비 구매 요청
    /// </summary>
    private void HandleShopItemClicked(ShopItem shopItem)
    {
        if (shopItem == null || shopItem.EquipmentData == null)
        {
            Debug.LogWarning("구매할 장비 데이터가 없습니다.");
            return;
        }

        if (_equipmentManager == null)
        {
            Debug.LogError("EquipmentManager가 할당되지 않았습니다.", this);
            return;
        }

        _equipmentManager.TryPurchaseOrEquip(shopItem.EquipmentData);
    }
    #endregion
}