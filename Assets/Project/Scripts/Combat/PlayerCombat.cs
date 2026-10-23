using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : MonoBehaviour
{
    #region 내부 변수
    private PlayerStats _playerStats;
    private PlayerAttackDetector _attackDetector;
    private PlayerStageActivator _activator;

    private bool _isAttacking;
    private bool _hasHitThisAttack;

    private float _attackElapsed;
    private float _nextAttackTime;
    #endregion

    #region 프로퍼티
    public bool IsAttacking => _isAttacking;

    /// <summary>
    /// 스테이지가 진행 중이고 Playing 상태일 때만 true
    /// </summary>
    public bool CanAct => _activator != null && _activator.CanAct;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        _attackDetector = GetComponent<PlayerAttackDetector>();
        _activator = GetComponent<PlayerStageActivator>();
    }

    private void OnDisable()
    {
        _isAttacking = false;
        _hasHitThisAttack = false;

        _attackElapsed = 0f;
        _nextAttackTime = 0f;
    }

    /// <summary>
    /// 행동 가능한 상태에서 자동 공격을 진행한다.
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

        if (Time.time < _nextAttackTime)
        {
            return;
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
        return CanAct
            && _attackDetector != null
            && !_isAttacking
            && Time.time >= _nextAttackTime;
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

        // 공격 시작 시점을 기준으로 다음 공격 시각을 예약한다.
        _nextAttackTime = Time.time + _playerStats.AttackInterval;

        Debug.Log("망치 공격 시작");
    }

    private void UpdateAttack()
    {
        _attackElapsed += Time.deltaTime;

        float attackDuration = _playerStats.AttackDuration;
        float hitTime = _playerStats.HitTime;

        // 타격 시점에 한 번만 범위 판정을 실행한다.
        if (!_hasHitThisAttack && _attackElapsed >= hitTime)
        {
            _hasHitThisAttack = true;

            int hitCount = _attackDetector.PerformAttack();

            Debug.Log($"망치 공격 판정 완료: {hitCount}개 대상 타격");

            // 타격 판정 중 컴포넌트가 비활성화됐다면
            // 이후 공격 처리를 진행하지 않는다.
            if (!isActiveAndEnabled)
            {
                return;
            }
        }

        if (_attackElapsed >= attackDuration)
        {
            EndAttack();
        }
    }

    private void EndAttack()
    {
        _isAttacking = false;
        _attackElapsed = 0f;

        // 다음 공격 시각은 BeginAttack에서 이미 예약했다.
        Debug.Log("망치 공격 완료");
    }
    #endregion
}