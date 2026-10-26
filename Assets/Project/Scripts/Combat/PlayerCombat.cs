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
    private float _cooldownRemaining;
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

        if (_activator == null)
        {
            Debug.LogWarning("PlayerStageActivator가 없어 공격하지 않음.");
        }
    }

    private void OnDisable()
    {
        _isAttacking = false;
        _hasHitThisAttack = false;

        _attackElapsed = 0f;
        _cooldownRemaining = 0f;
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
        _attackElapsed += Time.deltaTime;

        float attackDuration = _playerStats.AttackDuration;
        float hitTime = _playerStats.HitTime;

        // 타격 시점에 한 번만 범위 판정을 실행
        if (!_hasHitThisAttack && _attackElapsed >= hitTime)
        {
            _hasHitThisAttack = true;

            int hitCount = _attackDetector.PerformAttack();

            Debug.Log($"망치 공격 판정 완료: {hitCount}개 대상 타격");

            // 타격 판정 중 컴포넌트가 비활성화됐다면
            // 이후 공격 처리를 진행하지 않음.
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

    /// <summary>
    /// [PL-COMBAT-03] 공격 동작이 끝난 뒤 최종 공격 간격만큼 대기
    /// </summary>
    private void EndAttack()
    {
        _isAttacking = false;
        _attackElapsed = 0f;

        _cooldownRemaining = Mathf.Max(0f, _playerStats.AttackInterval);

        Debug.Log($"망치 공격 완료: 다음 공격까지 {_cooldownRemaining:F2}초");
    }
    #endregion
}