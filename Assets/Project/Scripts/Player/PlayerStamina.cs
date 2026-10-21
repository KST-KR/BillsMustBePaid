using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerStamina : MonoBehaviour
{
    #region 인스펙터
    [Header("스태미나 설정")]
    [SerializeField] float _initialStamina = 100f;
    #endregion

    #region 내부 변수
    private PlayerStats _playerStats;

    private float _currentStamina;
    private float _cachedMaxStamina;

    private bool _isDepleted;
    #endregion

    #region 프로퍼티
    public float CurrentStamina
    {
        get
        {
            return _currentStamina;
        }
    }

    public float MaxStamina
    {
        get
        {
            return _cachedMaxStamina;
        }
    }

    public bool IsDepleted
    {
        get
        {
            return _isDepleted;
        }
    }
    #endregion

    #region 이벤트
    /// <summary>
    /// 현재 또는 최대 스태미나가 변경되었을 때 UI에 알림 
    /// </summary>
    public event Action<float, float> OnStaminaChanged;

    /// <summary>
    /// 스태미나가 0이 되었을 떄 사망 조건을 전달 
    /// </summary>
    public event Action OnStaminaDepleted;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        _cachedMaxStamina = Mathf.Max(0f, _playerStats.MaxStamina);
        _currentStamina = Mathf.Clamp(_initialStamina, 0f, _cachedMaxStamina);
    }

    /// <summary>
    /// 비활성화된 동안 변경된 최대 스태미나도 반영
    /// </summary>
    private void OnEnable()
    {
        _playerStats.OnStateChanged += HandleStatsChanged;

        HandleStatsChanged();
    }

    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.OnStateChanged -= HandleStatsChanged;
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 스태미나 소모
    /// </summary>
    public float ConsumeStamina(float amount)
    {
        if (amount < 0f)
        {
            return 0f;
        }

        // 이미 고갈된 경우 추가 소모를 처리하지 않음.
        if (_isDepleted)
        {
            return 0f;
        }

        float consumedAmount = Mathf.Min(amount, _currentStamina);

        if (consumedAmount <= 0f)
        {
            NotifyDepletedOnce();
            return 0f;
        }

        _currentStamina -= consumedAmount;

        NotifyStaminaChanged();

        if (_currentStamina <= 0f)
        {
            _currentStamina = 0f;
            NotifyDepletedOnce();
        }

        return consumedAmount;
    }

    /// <summary>
    /// 스태미나 회복
    /// </summary>
    public float RecoverStamina(float amount)
    {
        if (amount <= 0f || _isDepleted)
        {
            return 0f;
        }

        float previousStamina = _currentStamina;

        _currentStamina = Mathf.Min(_currentStamina + amount, _cachedMaxStamina);

        float recoveredAmount = _currentStamina - previousStamina;

        if (recoveredAmount > 0f)
        {
            NotifyStaminaChanged();
        }
        
        return recoveredAmount;
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 능력치 변경
    /// </summary>
    private void HandleStatsChanged()
    {
        float newMaxStamina = Mathf.Max(0f, _playerStats.MaxStamina);

        bool maxChanged = !Mathf.Approximately(_cachedMaxStamina, newMaxStamina);

        if (!maxChanged)
        {
            return;
        }

        // 변경된 최대 스태미나를 반영
        _cachedMaxStamina = newMaxStamina;

        // 최대 스태미나가 감소하면 현재 스태미나도 제한한다.
        _currentStamina = Mathf.Min(_currentStamina, _cachedMaxStamina);

        NotifyStaminaChanged();

        if (_currentStamina <= 0f)
        {
            NotifyDepletedOnce();
        }
    }

    /// <summary>
    /// 이벤트 전달
    /// </summary>
    private void NotifyStaminaChanged()
    {
        OnStaminaChanged?.Invoke(_currentStamina, _cachedMaxStamina);
    }

    /// <summary>
    /// [PL-Stamina-07] 스태미나 변경 이벤트 알림
    /// </summary>
    private void NotifyDepletedOnce()
    {
        if (_isDepleted)
        {
            return;
        }

        _isDepleted = true;

        OnStaminaDepleted?.Invoke();
    }
    #endregion
}
