using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PL-STAT-01] 플레이어의 기본 능력치와 초기화
/// 성장 능력치를 받아와서 기본 능력치에 합한 후 반환
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

    [Header("성장 적용값")]
    [SerializeField] private float _attackPowerGrowth;
    [SerializeField] private float _attackIntervalModifier;
    [SerializeField] private float _attackRangeGrowth;
    [SerializeField] private float _criticalChanceGrowth;
    [SerializeField] private float _luckGrowth;
    [SerializeField] private float _maxStaminaGrowth;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 각 기본 능력치 + 성장 능력치 합
    /// </summary>
    public float AttackPower
    {
        get
        {
            return _baseAttackPower + _attackPowerGrowth;
        }
    }

    public float AttackInterval
    {
        get
        {
            return _baseAttackInterval + _attackIntervalModifier;
        }
    }

    public float AttackRange
    {
        get
        {
            return _baseAttackRange + _attackRangeGrowth;
        }
    }

    public float CriticalChance
    {
        get
        {
            return Mathf.Clamp01(_baseCriticalChance + _criticalChanceGrowth);
        }
    }

    public float Luck
    {
        get
        {
            return _baseLuck + _luckGrowth;
        }
    }

    public float MaxStamina
    {
        get
        {
            return _baseMaxStamina + _maxStaminaGrowth;
        }
    }
    #endregion

    #region 이벤트
    public event Action OnStateChanged;
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 현재 플레이의 성장값을 초기 상태로 되돌린다.
    /// 스테이지 전환이 아니라 새로운 플레이를 시작할 때 호출
    /// </summary>
    public void ResetRunGrowth()
    {
        _attackPowerGrowth = 0f;
        _attackIntervalModifier = 0f;
        _attackRangeGrowth = 0f;
        _criticalChanceGrowth = 0f;
        _luckGrowth = 0f;
        _maxStaminaGrowth = 0f;

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 각각 능력치에 따른 증가와 이벤트 전달
    /// </summary>
    public void AddAttackPowerGrowth(float amount)
    {
        _attackPowerGrowth += amount;
        OnStateChanged?.Invoke();
    }

    public void AddAttackIntervalModifier(float amount)
    {
        if (_baseAttackInterval + _attackIntervalModifier + amount <= 0f)
        {
            return;
        }

        _attackIntervalModifier += amount;
        OnStateChanged?.Invoke();
    }

    public void AddAttackRangeGrowth(float amount)
    {
        if (_baseAttackRange + _attackRangeGrowth + amount <= 0f)
        {
            return;
        }

        _attackRangeGrowth += amount;
        OnStateChanged?.Invoke();
    }

    public void AddCriticalChanceGrowth(float amount)
    {
        _criticalChanceGrowth += amount;
        OnStateChanged?.Invoke();
    }

    public void AddLuckGrowth(float amount)
    {
        _luckGrowth += amount;
        OnStateChanged?.Invoke();
    }

    public void AddMaxStaminaGrowth(float amount)
    {
        if (_baseMaxStamina + _maxStaminaGrowth + amount <= 0f)
        {
            return;
        }

        _maxStaminaGrowth += amount;
        OnStateChanged?.Invoke();
    }
    #endregion

}
