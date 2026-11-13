using System;
using UnityEngine;

/// <summary>
/// [PL-STAT-01] 플레이어의 기본 능력치와 최종 능력치 계산
/// GrowthManager.OnApplyStatGrowth로 전달되는 변경분(amount)을 성장값에 누적
/// 최종 능력치 = 기본 능력치 + 누적된 성장값 + 장비 능력치 + 영구 성장값
/// </summary>
public class PlayerStats : MonoBehaviour
{
    #region 상수
    private const string CAttackSpeedActiveSkillId = "ACT_03"; 
    #endregion

    #region 인스펙터
    [Header("기본 능력치")]
    [SerializeField] private float _baseAttackPower = 10f;
    [SerializeField] private float _baseAttackInterval = 0.5f;
    [SerializeField] private float _baseAttackRange = 2f;
    [SerializeField] private float _baseCriticalChance = 0f;
    [SerializeField] private float _baseCriticalDamageMultiplier = 2f;
    [SerializeField] private float _baseLuck = 0f;
    [SerializeField] private float _baseMaxStamina = 100f;

    [Header("최소값 보정")]
    [SerializeField] private float _minAttackInterval = 0.05f;
    [SerializeField] private float _minAttackRange = 0.1f;
    [SerializeField] private float _minMaxStamina = 1f;

    [Header("공격 동작 설정")]
    [SerializeField] private float _attackDuration = 0.8f;
    [SerializeField] private float _hitTime = 0.4f;
    #endregion

    #region 내부 변수
    // OnApplyStatGrowth로 누적된 성장 합산값
    private float _attackPowerGrowth;
    private float _attackIntervalGrowth;
    private float _attackRangeGrowth;
    private float _criticalChanceGrowth;
    private float _luckGrowth;
    private float _maxStaminaGrowth;

    // 장착한 망치의 능력치
    private float _hammerAttackPower;
    private float _hammerAttackInterval;
    private float _hammerAttackRange;
    private float _hammerCriticalChance;
    private float _hammerCriticalDamageBonus;
    private float _hammerStaminaDrainMultiplier = 1f;
    private float _activeAttackSpeedBonusRate;

    private float _perkAttackFlat;
    private float _perkAttackRate;
    private float _perkAttackSpeedFlat;
    private float _perkAttackSpeedRate;
    private float _perkAttackRangeFlat;
    private float _perkAttackRangeRate;
    private float _perkCriticalFlat;
    private float _perkCriticalRate;
    private float _perkStaminaFlat;
    private float _perkStaminaRate;

    private GrowthManager _growthManager;
    private LegacyShopManager _legacyShopManager;
    private PerkManager _perkManager;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 최종 공격력
    /// </summary>
    public float AttackPower
    {
        get
        {
            float baseValue = _baseAttackPower + _attackPowerGrowth + _hammerAttackPower + GetLegacyShopBonus(EUpgradeStat.Attack);
            return (baseValue + _perkAttackFlat) * (1.0f + Mathf.Max(0.0f, _perkAttackRate));
        }
    }

    /// <summary>
    /// 최종 공격 간격
    /// </summary>
    public float AttackInterval
    {
        get
        {
            float baseInterval = Mathf.Max(_minAttackInterval,
                _baseAttackInterval + _attackIntervalGrowth + _hammerAttackInterval + GetLegacyShopBonus(EUpgradeStat.AttackSpeed));
            baseInterval = Mathf.Max(_minAttackInterval, baseInterval - Mathf.Max(0.0f, _perkAttackSpeedFlat));
            baseInterval = Mathf.Max(_minAttackInterval, baseInterval / (1.0f + Mathf.Max(0.0f, _perkAttackSpeedRate)));

            if (_activeAttackSpeedBonusRate <= 0f)
            {
                return baseInterval;
            }

            return Mathf.Max(_minAttackInterval, baseInterval / (1.0f + _activeAttackSpeedBonusRate));
        }
    }

    /// <summary>
    /// 최종 타격 판정 범위
    /// </summary>
    public float AttackRange
    {
        get
        {
            float baseValue = Mathf.Max(_minAttackRange, _baseAttackRange + _attackRangeGrowth + _hammerAttackRange + GetLegacyShopBonus(EUpgradeStat.AttackRange));
            return Mathf.Max(_minAttackRange, (baseValue + _perkAttackRangeFlat) * (1.0f + Mathf.Max(0.0f, _perkAttackRangeRate)));
        }
    }

    /// <summary>
    /// 최종 치명타 확률
    /// </summary>
    public float CriticalChance
    {
        get
        {
            float baseValue = _baseCriticalChance + _criticalChanceGrowth + _hammerCriticalChance + GetLegacyShopBonus(EUpgradeStat.CriticalChance);
            return Mathf.Clamp01((baseValue + _perkCriticalFlat) * (1.0f + Mathf.Max(0.0f, _perkCriticalRate)));
        }
    }

    /// <summary>
    /// 최종 치명타 피해 배율
    /// </summary>
    public float CriticalDamageMultiplier => _baseCriticalDamageMultiplier + _hammerCriticalDamageBonus;

    public float Luck => _baseLuck + _luckGrowth + GetLegacyShopBonus(EUpgradeStat.Luck);

    public float MaxStamina => Mathf.Max(_minMaxStamina,
        (_baseMaxStamina + _maxStaminaGrowth + GetLegacyShopBonus(EUpgradeStat.Stamina) + _perkStaminaFlat) * (1.0f + Mathf.Max(0.0f, _perkStaminaRate)));

    /// <summary>
    /// 장착한 망치의 스태미나 소모 배율
    /// </summary>
    public float StaminaDrainMultiplier => _hammerStaminaDrainMultiplier;

    /// <summary>
    /// 한 번의 공격 동작 시간
    /// </summary>
    public float AttackDuration => Mathf.Max(0f, _attackDuration);

    /// <summary>
    /// 공격 시작 후 타격 판정 시점
    /// </summary>
    public float HitTime => Mathf.Clamp(_hitTime, 0f, AttackDuration);
    #endregion

    #region 이벤트
    /// <summary>
    /// 성장값이 바뀌어 최종 능력치가 변했을 때 발생
    /// </summary>
    public event Action OnStateChanged;
    #endregion

    #region 이벤트 함수
    /// <summary>
    /// 다른 매니저의 Awake가 끝난 뒤인 Start에서 구독
    /// 구독 전에 발생한 변경분(먼저 끝난 불러오기 등)은 놓치므로,
    /// 성장값을 0으로 만든 뒤 GrowthManager가 보유 스킬을 다시 전달 하도록 요청
    /// </summary>
    private void OnEnable()
    {
        InitActiveSkillEvents();
        SyncActiveAttackSpeedState();
    }

    private void Start()
    {
        _growthManager = GrowthManager.Instance;
        _legacyShopManager = LegacyShopManager.Instance;
        _perkManager = PerkManager.Instance;

        if (_legacyShopManager != null)
        {
            _legacyShopManager.OnOwnershipDataChanged += HandleLegacyShopDataChanged;
        }

        if (_growthManager == null)
        {
            Debug.LogError("GrowthManager가 없어 성장값 없이 시작");
            return;
        }

        _growthManager.OnApplyStatGrowth += HandleApplyStatGrowth;
        _growthManager.OnResetRunGrowth += ResetGrowth;

        if (_perkManager != null)
        {
            _perkManager.OnPerkStateChanged += HandlePerkStateChanged;
        }

        RebuildGrowth();
        RebuildPerkEffects();
        InitActiveSkillEvents();
        SyncActiveAttackSpeedState();
    }

    private void OnDisable()
    {
        InitActiveSkillEvents(false);
    }

    private void OnDestroy()
    {
        if (_growthManager != null)
        {
            _growthManager.OnApplyStatGrowth -= HandleApplyStatGrowth;
            _growthManager.OnResetRunGrowth -= ResetGrowth;
            _growthManager = null;
        }

        if (_perkManager != null)
        {
            _perkManager.OnPerkStateChanged -= HandlePerkStateChanged;
            _perkManager = null;
        }

        if (_legacyShopManager != null)
        {
            _legacyShopManager.OnOwnershipDataChanged -= HandleLegacyShopDataChanged;
            _legacyShopManager = null;
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 성장값을 모두 0으로 되돌리고 변경 이벤트를 발생
    /// 새로운 런 시작, 저장 데이터 불러오기 직전에 GrowthManager가 호출하게 함.
    /// </summary>
    public void ResetGrowth()
    {
        ClearGrowthValues();
        ClearPerkValues();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 성장값을 0으로 만든 뒤 GrowthManager가 보유 스킬의 성장값을 다시 전달하게 만듦.
    /// 값이 어긋났을 때의 복구 수단
    /// </summary>
    [ContextMenu("성장값 초기화 후 재적용")]
    public void RebuildGrowth()
    {
        ClearGrowthValues();

        if (_growthManager != null)
        {
            _growthManager.ReapplyPlayerStatSkills();
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 망치 장착시 장비 능력치 교체
    /// </summary>
    /// <param name="data"></param>
    public void SetHammerEquipment(EquipmentData data)
    {
        _hammerAttackPower = data != null ? data.AttackPower : 0f;
        _hammerAttackInterval = data != null ? data.AttackInterval : 0f;
        _hammerAttackRange = data != null ? data.AttackRange : 0f;
        _hammerCriticalChance = data != null ? data.CriticalChance : 0f;

        _hammerCriticalDamageBonus = data != null ? data.CriticalDamageBonus : 0f;

        _hammerStaminaDrainMultiplier = data != null ? Mathf.Max(0f, data.StaminaDrainMultiplier) : 1f;

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 장비 교체 시 예상 공격력을 계산
    /// </summary>
    public float GetAttackPowerWithEquipment(EquipmentData data)
    {
        float equipmentValue = data != null ? data.AttackPower : 0f;

        return _baseAttackPower + _attackPowerGrowth + equipmentValue + GetLegacyShopBonus(EUpgradeStat.Attack);
    }

    /// <summary>
    /// 장비 교체 시 예상 공격 간격을 계산
    /// </summary>
    public float GetAttackIntervalWithEquipment(EquipmentData data)
    {
        float equipmentValue = data != null ? data.AttackInterval : 0f;

        return Mathf.Max(_minAttackInterval, _baseAttackInterval + _attackIntervalGrowth + equipmentValue + GetLegacyShopBonus(EUpgradeStat.AttackSpeed));
    }

    /// <summary>
    /// 장비 교체 시 예상 공격 범위를 계산
    /// </summary>
    public float GetAttackRangeWithEquipment(EquipmentData data)
    {
        float equipmentValue = data != null ? data.AttackRange : 0f;

        return Mathf.Max(_minAttackRange, _baseAttackRange + _attackRangeGrowth + equipmentValue + GetLegacyShopBonus(EUpgradeStat.AttackRange));
    }

    public float GetCriticalChanceWithEquipment(EquipmentData data)
    {
        float equipmentValue = data != null ? data.CriticalChance : 0f;

        return Mathf.Clamp01(_baseCriticalChance + _criticalChanceGrowth + equipmentValue + GetLegacyShopBonus(EUpgradeStat.CriticalChance));
    }

    /// <summary>
    /// 장비 교체 시 예상 치명타 피해 배율을 계산
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public float GetCriticalDamageMultiplierWithEquipment(EquipmentData data)
    {
        float equipmentValue = data != null ? data.CriticalDamageBonus : 0f;

        return _baseCriticalDamageMultiplier + equipmentValue;
    }

    /// <summary>
    /// 장비 교체시 예상 스태미나 소모 배율을 계산
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public float GetStaminaDrainMultiplierWithEquipment(EquipmentData data)
    {
        return data != null ? Mathf.Max(0f, data.StaminaDrainMultiplier) : 1f;
    }
    #endregion

    #region 커스텀 (private)
    private float GetLegacyShopBonus(EUpgradeStat stat)
    {
        return LegacyShopManager.Instance != null ? LegacyShopManager.Instance.GetStatBonus(stat) : 0.0f;
    }

    private void HandleLegacyShopDataChanged()
    {
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// GrowthManager.OnApplyStatGrowth처리
    /// amount는 이번에 변경된 값이므로 해당 능력치에 더함.
    /// </summary>
    /// <param name="upgrade"></param>
    /// <param name="amount"></param>
    private void HandleApplyStatGrowth(UpgradeData upgrade, float amount)
    {
        if (upgrade == null || Mathf.Approximately(amount, 0f))
        {
            return;
        }

        switch (upgrade.TargetStat)
        {
            case EUpgradeStat.Attack:
                _attackPowerGrowth += amount;
                break;

            // AttackSpeed 스킬은 레벨당 음수 값이므로 더함.
            case EUpgradeStat.AttackSpeed:
                _attackIntervalGrowth += amount;
                break;

            case EUpgradeStat.AttackRange:
                _attackRangeGrowth += amount;
                break;

            case EUpgradeStat.Luck:
                _luckGrowth += amount;
                break;
            
            case EUpgradeStat.CriticalChance:
                _criticalChanceGrowth += amount; 
                break;

            case EUpgradeStat.Stamina:
                _maxStaminaGrowth += amount; 
                break;

            default:
                Debug.LogWarning($"처리되지 않은 능력치 성장: {upgrade.TargetStat}");
                return;
        }

        OnStateChanged?.Invoke();
    }

    private void ClearGrowthValues()
    {
        _attackPowerGrowth = 0f;
        _attackIntervalGrowth = 0f;
        _attackRangeGrowth = 0f;
        _criticalChanceGrowth = 0f;
        _luckGrowth = 0f;
        _maxStaminaGrowth = 0f;
    }

    private void HandlePerkStateChanged()
    {
        RebuildPerkEffects();
        OnStateChanged?.Invoke();
    }

    private void RebuildPerkEffects()
    {
        if (_perkManager == null)
        {
            _perkManager = PerkManager.Instance;
        }

        ClearPerkValues();
        if (_perkManager == null)
        {
            return;
        }

        _perkAttackFlat = _perkManager.GetTotalFlat(EPerkTargetStat.Attack);
        _perkAttackRate = _perkManager.GetTotalPercent(EPerkTargetStat.Attack);
        _perkAttackSpeedFlat = _perkManager.GetTotalFlat(EPerkTargetStat.AttackSpeed);
        _perkAttackSpeedRate = _perkManager.GetTotalPercent(EPerkTargetStat.AttackSpeed);
        _perkAttackRangeFlat = _perkManager.GetTotalFlat(EPerkTargetStat.AttackRange);
        _perkAttackRangeRate = _perkManager.GetTotalPercent(EPerkTargetStat.AttackRange);
        _perkCriticalFlat = _perkManager.GetTotalFlat(EPerkTargetStat.CriticalChance);
        _perkCriticalRate = _perkManager.GetTotalPercent(EPerkTargetStat.CriticalChance);
        _perkStaminaFlat = _perkManager.GetTotalFlat(EPerkTargetStat.Stamina);
        _perkStaminaRate = _perkManager.GetTotalPercent(EPerkTargetStat.Stamina);
    }

    private void ClearPerkValues()
    {
        _perkAttackFlat = 0.0f;
        _perkAttackRate = 0.0f;
        _perkAttackSpeedFlat = 0.0f;
        _perkAttackSpeedRate = 0.0f;
        _perkAttackRangeFlat = 0.0f;
        _perkAttackRangeRate = 0.0f;
        _perkCriticalFlat = 0.0f;
        _perkCriticalRate = 0.0f;
        _perkStaminaFlat = 0.0f;
        _perkStaminaRate = 0.0f;
    }

    private void InitActiveSkillEvents(bool isAdd = true)
    {
        if (!ActiveSkillManager.HasInstance)
        {
            return;
        }

        ActiveSkillManager.Instance.OnActiveBuffStarted -= HandleActiveBuffStarted;
        ActiveSkillManager.Instance.OnActiveBuffRefreshed -= HandleActiveBuffRefreshed;
        ActiveSkillManager.Instance.OnActiveBuffEnded -= HandleActiveBuffEnded;

        if (isAdd)
        {
            ActiveSkillManager.Instance.OnActiveBuffStarted += HandleActiveBuffStarted;
            ActiveSkillManager.Instance.OnActiveBuffRefreshed += HandleActiveBuffRefreshed;
            ActiveSkillManager.Instance.OnActiveBuffEnded += HandleActiveBuffEnded;
        }
    }

    private void SyncActiveAttackSpeedState()
    {
        if (!ActiveSkillManager.HasInstance)
        {
            _activeAttackSpeedBonusRate = 0f;
            return;
        }

        bool isBuffActive = ActiveSkillManager.Instance.IsBuffActive(CAttackSpeedActiveSkillId);
        ActionACT03(CAttackSpeedActiveSkillId, isBuffActive);
    }

    private void ActionACT03(string upgradeId, bool isActive)
    {
        if (upgradeId != CAttackSpeedActiveSkillId)
        {
            return;
        }

        float previous = _activeAttackSpeedBonusRate;
        _activeAttackSpeedBonusRate = isActive ? GetACT03BonusRate() : 0f;

        if (!Mathf.Approximately(previous, _activeAttackSpeedBonusRate))
        {
            OnStateChanged?.Invoke();
        }
    }

    private float GetACT03BonusRate()
    {
        GrowthManager growthManager = GrowthManager.Instance;
        if (growthManager == null)
        {
            return 0f;
        }

        UpgradeData upgrade = growthManager.GetUpgrade(CAttackSpeedActiveSkillId);
        int level = growthManager.GetLevel(CAttackSpeedActiveSkillId);
        if (upgrade == null || level <= 0)
        {
            return 0f;
        }

        return Mathf.Max(0f, upgrade.ValuePerLevel * level);
    }

    private void HandleActiveBuffStarted(string upgradeId, float duration)
    {
        ActionACT03(upgradeId, true);
    }

    private void HandleActiveBuffRefreshed(string upgradeId, float duration)
    {
        ActionACT03(upgradeId, true);
    }

    private void HandleActiveBuffEnded(string upgradeId)
    {
        ActionACT03(upgradeId, false);
    }
    #endregion
}
