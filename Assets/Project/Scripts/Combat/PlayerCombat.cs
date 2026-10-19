using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : MonoBehaviour
{
    #region 인스펙터
    [Header("임시 공격 타이밍")]
    [SerializeField] private float _attackDuration = 0.8f;
    [SerializeField] private float _hitTime = 0.4f;

    [Header("테스트 설정")]
    [SerializeField] private bool _startCombatOnEnable = true;
    #endregion

    #region 내부 변수
    private PlayerStats _playerStats;
    private PlayerAttackDetector _attackDetector;

    private bool _combatEnabled;
    private bool _isAttacking;
    private bool _hasHitThisAttack;

    private float _attackElapsed;
    private float _cooldownRemaining;
    #endregion

    #region 프로퍼티
    public bool IsAttacking
    {
        get
        {
            return _isAttacking;
        }
    }
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        _attackDetector = GetComponent<PlayerAttackDetector>();
    }

    private void OnEnable()
    {
        _combatEnabled = _startCombatOnEnable;
    }

    private void OnDisable()
    {
        _combatEnabled = false;
        _isAttacking = false;
        _hasHitThisAttack = false;

        _attackElapsed = 0f;
        _cooldownRemaining = 0f;
    }

    /// <summary>
    /// 공격 진행
    /// </summary>
    void Update()
    {
        if (!_combatEnabled || _attackDetector == null)
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
    private void BeginAttack()
    {
        _isAttacking = true;
        _hasHitThisAttack = false;
        _attackElapsed = 0f;

        Debug.Log("망치 공격 시작");
    }

    private void UpdateAttack()
    {
        _attackElapsed += Time.deltaTime;

        float attackDuration = Mathf.Max(0f, _attackDuration);
        float hitTime = Mathf.Clamp(_hitTime, 0f, attackDuration);

        // 타격 시점에 한 번만 범위 판정을 실행
        if (!_hasHitThisAttack && _attackElapsed >= hitTime)
        {
            _hasHitThisAttack = true;

            int hitCount = _attackDetector.PerformAttack();

            Debug.Log($"망치 공격 판정 완료 : {hitCount}개 대상 타격");
        }

        // 공격 동작이 끝난 뒤 다음 공격 간격을 계산
        if (_attackElapsed >= attackDuration)
        {
            EndAttack();
        }
    }

    private void EndAttack()
    {
        _isAttacking = false;
        _attackElapsed = 0f;

        _cooldownRemaining = Mathf.Max(0f, _playerStats.AttackInterval);

        Debug.Log($"망치 공격 완료 : 다음 공격 까지 {_cooldownRemaining:F2}초");
    }
    #endregion

    #region 커스텀 (public)
    public void SetCombatEnabled(bool isEnabled)
    {
        _combatEnabled = isEnabled;

        if (!isEnabled)
        {
            _isAttacking = false;
            _hasHitThisAttack = false;

            _attackElapsed = 0f;
            _cooldownRemaining = 0f;
        }
    }
    #endregion

}
