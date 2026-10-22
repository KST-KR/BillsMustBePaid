using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PL-STAT-01] 플레이어의 기본 능력치와 최종 능력치 계산
/// GrowthManager.OnApplyStatGrowth로 전달되는 변경분(amount)을 성장값에 누적
/// 최종 능력치 = 기본 능력치 + 누적된 성장값 (+ 최소값 보정)
/// </summary>
public class PlayerStats : MonoBehaviour
{
    #region 인스펙터
    [Header("기본 능력치")]
    [SerializeField] private float _baseAttackPower = 10f;
    [SerializeField] private float _baseAttackInterval = 0.5f;
    [SerializeField] private float _baseAttackRange = 2f;
    [SerializeField] private float _baseCriticalChance = 0f;
    [SerializeField] private float _baseLuck = 0f;
    [SerializeField] private float _baseMaxStamina = 100f;

    [Header("최소값 보정")]
    [SerializeField] private float _minAttackInterval = 0.05f;
    [SerializeField] private float _minAttackRange = 0.1f;
    [SerializeField] private float _minMaxStamina = 1f;
    #endregion

    #region 내부 변수
    // OnApplyStatGrowth로 누적된 성장 합산값
    private float _attackPowerGrowth;
    private float _attackIntervalGrowth;
    private float _attackRangeGrowth;
    private float _criticalChanceGrowth;
    private float _luckGrowth;
    private float _maxStaminaGrowth;

    private GrowthManager _growthManager;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 각 기본 능력치 + 성장 능력치 합
    /// </summary>
    public float AttackPower => _baseAttackPower + _attackPowerGrowth;

    public float AttackInterval => Mathf.Max(_minAttackInterval, _baseAttackInterval + _attackIntervalGrowth);

    public float AttackRange => Mathf.Max(_minAttackRange, _baseAttackRange + _attackRangeGrowth);

    public float CriticalChance => Mathf.Clamp01(_baseCriticalChance + _criticalChanceGrowth);

    public float Luck => _baseLuck + _luckGrowth;

    public float MaxStamina => Mathf.Max(_minMaxStamina, _baseMaxStamina + _maxStaminaGrowth);
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
    private void Start()
    {
        _growthManager = GrowthManager.Instance;

        if (_growthManager == null)
        {
            Debug.LogError("GrowthManager가 없어 성장값 없이 시작");
            return;
        }

        _growthManager.OnApplyStatGrowth += HandleApplyStatGrowth;
        // GrowthManager에 OnResetRunGrowth 이벤트가 추가되면 활성화 (런 초기화, 불러오기 시작 시점에 호출됨)
        _growthManager.OnResetRunGrowth += ResetGrowth;

        RebuildGrowth();
    }

    private void OnDestroy()
    {
        if (_growthManager != null)
        {
            _growthManager.OnApplyStatGrowth -= HandleApplyStatGrowth;
            _growthManager.OnResetRunGrowth -= ResetGrowth;
            _growthManager = null;
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
    #endregion

    #region 커스텀 (private)
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

            /*
            TODO : EUpgradeStat에 치명타 확률, 최대 스태미나 항목이 추가되면 활성화
            case EUpgradeStat.CriticalChance:
                _criticalChanceGrowth += amount; 
                break;

            case EUpgradeStat.Stamina:
                _maxStaminaGrowth += amount; 
                break;
            */

            default:
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

    #endregion

}
