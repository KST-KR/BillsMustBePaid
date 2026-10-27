using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : MonoBehaviour
{
    #region 내부 변수
    private PlayerStats _playerStats;
    private PlayerStamina _playerStamina;
    private PlayerAttackDetector _attackDetector;
    private PlayerStageActivator _activator;

    private bool _isAttacking;
    private bool _hasHitThisAttack;

    private float _attackElapsed;
    // 공격 종료 후 다음 공격까지의 대기 시간곽 남은 시간
    // 에임의 진행도를 계산할 때 전체 대기 시간이 필요하므로 따로 보관
    private float _cooldownDuration;
    private float _cooldownRemaining;
    #endregion

    #region 이벤트
    /// <summary>
    /// 타격 판정이 실행될때 발생 (타격한 대상 수 정달)
    /// 에임의 타격 연출 등에서 구독
    /// </summary>
    public event Action<int> OnAttackPerformed;
    #endregion

    #region 프로퍼티
    public bool IsAttacking => _isAttacking;

    /// <summary>
    /// 스테이지가 진행 중이고 Playing 상태일 때만 true
    /// 스태미나가 고갈되지 않았을 때만 행동 가능
    /// </summary>
    public bool CanAct => _activator != null && _activator.CanAct && _playerStamina != null && !_playerStamina.IsExhausted;

    /// <summary>
    /// 에임의 실선 링 채움 정도 (0 ~ 1)
    /// 공격이 끝난 직후 0에서 시작해 대기 시간과 타격 직전까지의 시간동안 채워지고,
    /// 망치가 내려찍는 타격 시점에 1이 됨. 공격 간격이 짧을수록 빨리 채워짐.
    /// </summary>
    public float AimProgress
    {
        get
        {
            float hitTime = _playerStats.HitTime;
            float windUp = _cooldownDuration + hitTime;

            if (windUp <= 0f)
            {
                return 1f;
            }

            float elapsed = _isAttacking ? _cooldownDuration + Mathf.Min(_attackElapsed, hitTime) : _cooldownDuration - _cooldownRemaining;

            return Mathf.Clamp01(elapsed / windUp);
        }
    }
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        _playerStamina = GetComponent<PlayerStamina>();
        _attackDetector = GetComponent<PlayerAttackDetector>();
        _activator = GetComponent<PlayerStageActivator>();

        if (_activator == null)
        {
            Debug.LogWarning("PlayerStageActivator가 없어 공격하지 않음.");
        }

        if (_playerStamina == null)
        {
            Debug.LogWarning("PlayerStamina가 없어 공격하지 않음.");
        }
    }

    private void OnEnable()
    {
        if (_playerStamina != null)
        {
            _playerStamina.OnExhausted += HandleStaminaExhausted;

            // 이미 고갈된 상태에서 활성화된 경우도 처리
            if (_playerStamina.IsExhausted)
            {
                HandleStaminaExhausted();
            }
        }

        StartCooldown();
    }

    private void OnDisable()
    {
        if (_playerStamina != null)
        {
            _playerStamina.OnExhausted -= HandleStaminaExhausted;
        }

        ResetAttackState();
    }

    /// <summary>
    /// 행동 가능한 상태에서 자동 공격을 진행
    /// </summary>
    private void Update()
    {
        if (!CanAct || _attackDetector == null)
        {
            return;
        }

        if (_isAttacking)
        {
            UpdateAttack();
            return;
        }

        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= Time.deltaTime;

            if (_cooldownRemaining > 0f)
            {
                return;
            }

            _cooldownRemaining = 0f;
        }

        BeginAttack();
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// [PL-COMBAT-01] 자동 공격 시작 조건
    /// </summary>
    private bool CanStartAttack()
    {
        // 1. 스테이지가 진행 중이고 게임 상태가 Playing인가?
        if (!CanAct)
        {
            return false;
        }

        // 2. 공격 판정기가 존재하는가?
        if (_attackDetector == null)
        {
            return false;
        }

        // 3. 현재 공격 중이지 않은가?
        if (_isAttacking)
        {
            return false;
        }

        // 4. 다음 공격을 시작할 시간이 되었는가?
        if (_cooldownRemaining > 0f)
        {
            return false;
        }

        return true;
    }

    private void BeginAttack()
    {
        if (!CanStartAttack())
        {
            return;
        }

        _isAttacking = true;
        _hasHitThisAttack = false;
        _attackElapsed = 0f;

        Debug.Log("망치 공격 시작");
    }

    private void UpdateAttack()
    {
        // 공격 도중 스태미나가 고갈되면 타격 판정을 진행 X
        if (!CanAct)
        {
            ResetAttackState();
        }

        _attackElapsed += Time.deltaTime;

        float attackDuration = _playerStats.AttackDuration;
        float hitTime = _playerStats.HitTime;

        // 타격 시점에 한 번만 범위 판정을 실행
        if (!_hasHitThisAttack && _attackElapsed >= hitTime)
        {
            _hasHitThisAttack = true;

            int hitCount = _attackDetector.PerformAttack();

            OnAttackPerformed?.Invoke(hitCount);

            Debug.Log($"망치 공격 판정 완료: {hitCount}개 대상 타격");

            // 타격 판정 중 컴포넌트가 비활성화됐다면
            // 이후 공격 처리를 진행하지 않음.
            if (!isActiveAndEnabled)
            {
                return;
            }

            // 타격 판정 중 스태미나가 고갈된 경우에도 종료 처리
            if (!CanAct)
            {
                ResetAttackState();
            }
        }

        if (_attackElapsed >= attackDuration)
        {
            EndAttack();
        }
    }

    /// <summary>
    /// 스태미나 고갈 이벤트가 발생하면 현재 공격을 즉시 중단
    /// </summary>
    private void HandleStaminaExhausted()
    {
        ResetAttackState();
    }

    private void ResetAttackState()
    {
        _isAttacking = false;
        _hasHitThisAttack = false;

        _attackElapsed = 0f;
        _cooldownDuration = 0f;
        _cooldownRemaining = 0f;
    }

    /// <summary>
    /// [PL-COMBAT-03] 공격 동작이 끝난 뒤 최종 공격 간격만큼 대기
    /// </summary>
    private void EndAttack()
    {
        _isAttacking = false;
        _attackElapsed = 0f;

        StartCooldown();

        Debug.Log($"망치 공격 완료: 다음 공격까지 {_cooldownRemaining:F2}초");
    }

    private void StartCooldown()
    {
        _cooldownDuration = Mathf.Max(0f, _playerStats.AttackInterval);
        _cooldownRemaining = _cooldownDuration;
    }
    #endregion
}