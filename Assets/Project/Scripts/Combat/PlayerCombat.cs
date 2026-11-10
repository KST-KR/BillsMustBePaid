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
    private HammerController _hammerController;

    private bool _isAttacking;

    // 공격 종료 후 다음 공격까지의 대기 시간곽 남은 시간
    // 에임의 진행도를 계산할 때 전체 대기 시간이 필요하므로 따로 보관
    private float _cooldownDuration;
    private float _cooldownRemaining;

    private Animator _handAnimator;
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private HandStateMachine _handStateMachine;
    private IdleState _idleState;
    private SwingState _swingState;
    private HitState _hitState;
    private TiredState _tiredState;
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

            float elapsed = _isAttacking ? _cooldownDuration + Mathf.Min(_swingState.AttackElapsed, hitTime) : _cooldownDuration - _cooldownRemaining;

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

        _handAnimator = GetComponentInChildren<Animator>(true);
        _hammerController = GetComponentInChildren<HammerController>(true);

        if (_handAnimator == null)
        {
            Debug.LogWarning("HandPivot에 Animator가 없어 공격 애니메이션을 재생할 수 없음.");
        }

        if (_activator == null)
        {
            Debug.LogWarning("PlayerStageActivator가 없어 공격하지 않음.");
        }

        if (_playerStamina == null)
        {
            Debug.LogWarning("PlayerStamina가 없어 공격하지 않음.");
        }

        if (_hammerController == null)
        {
            Debug.LogWarning("HammerController를 찾을 수 없음.");
        }

        InitializeHandStates();
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

        _handStateMachine?.ChangeState(_idleState);
    }

    /// <summary>
    /// 행동 가능한 상태에서 자동 공격을 진행
    /// </summary>
    private void Update()
    {
        // 스테이지가 진행 중이고 게임 상태가 Playing인지 확인
        if (_activator == null || !_activator.CanAct)
        {
            return;
        }

        // 대기 상태일 때만 쿨다운 감소
        if (_handStateMachine.CurrentState == _idleState)
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;

                if (_cooldownRemaining < 0f)
                {
                    _cooldownRemaining = 0f;
                }
            }
        }

        // 현재 상태의 Update 실행
        _handStateMachine.Update();
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

        _swingState.StartAttack();
        _handStateMachine.ChangeState(_swingState);
    }

    /// <summary>
    /// 스태미나 고갈 이벤트가 발생하면 현재 공격을 즉시 중단
    /// </summary>
    private void HandleStaminaExhausted()
    {
        // 진행 중인 공격 상태 초기화
        ResetAttackState();

        // 공격 애니메이션 즉시 중단하고 대기 자세로 전환
        if (_handAnimator != null)
        {
            _handAnimator.ResetTrigger(AttackHash);
            _handAnimator.Play(IdleHash, 0, 0f);
        }

        // 장착된 망치 분리 및 낙하
        _hammerController?.DropHammer();

        // 휴직 상태로 전환
        _handStateMachine.ChangeState(_tiredState);

        Debug.Log("스태미나 고갈: 공격 중단 및 망치 분리");
    }

    private void HandleStaminaRecovered()
    {
        // 망치 재장착
        _hammerController?.AttachHammer();

        // 다음 공격까지의 대기 시간 초기화
        StartCooldown();

        // 공격 대기 상태로 전환
        _handStateMachine.ChangeState(_idleState);

        Debug.Log("스태미나 회복: 공격 대기 상태로 전환");
    }

    private void ResetAttackState()
    {
        _isAttacking = false;

        _swingState?.CancelAttack();

        _cooldownDuration = 0f;
        _cooldownRemaining = 0f;
    }

    /// <summary>
    /// [PL-COMBAT-03] 공격 동작이 끝난 뒤 최종 공격 간격만큼 대기
    /// </summary>
    private void EndAttack()
    {
        _isAttacking = false;

        StartCooldown();

        _handStateMachine.ChangeState(_idleState);

        Debug.Log($"망치 공격 완료: 다음 공격까지 {_cooldownRemaining:F2}초");
    }

    private void StartCooldown()
    {
        _cooldownDuration = Mathf.Max(0f, _playerStats.AttackInterval);
        _cooldownRemaining = _cooldownDuration;
    }

    /// <summary>
    /// 손 상태 객체를 생성하고 초기 상태를 설정
    /// </summary>
    private void InitializeHandStates()
    {
        _handStateMachine = new HandStateMachine();

        // 대기 상태
        _idleState = new IdleState(CanStartAttack, BeginAttack);

        // 공격 상태
        _swingState = new SwingState(
            () => _playerStats.HitTime,
            () => _playerStats.AttackDuration,
            () => _attackDetector.PerformAttack(),
            HandleAttackStarted,
            EndAttack,
            HandleAttackPerformed);

        // 스태미나 고갈 상태
        _tiredState = new TiredState(
            () => _playerStamina != null && _playerStamina.IsExhausted, HandleStaminaRecovered);

        // 피격 상태
        _hitState = new HitState(
            () => Debug.Log("타격 애니메이션 재생 완료 대기"),
            HandleHitCompleted);

        // 최초 상태 설정
        _handStateMachine.ChangeState(_idleState);
    }

    private void HandleAttackStarted()
    {
        _isAttacking  = true;

        if (_handAnimator != null)
        {
            _handAnimator.SetTrigger(AttackHash);
        }

        Debug.Log("망치 공격 시작");
    }

    private void HandleAttackPerformed(int hitCount)
    {
        OnAttackPerformed?.Invoke(hitCount);

        Debug.Log($"망치 공격 판정 완료: {hitCount}개 대상 타격");

        // 실제 대상을 타격한 경우에만 HitState로 전환
        if (hitCount > 0)
        {
            _handStateMachine.ChangeState(_hitState);
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// Animation Event를 통해 타격 애니메이션 완료 신호 수신
    /// </summary>
    public void OnHitAnimationCompleted()
    {
        // HitState가 아닌 경우 무시
        if (_handStateMachine.CurrentState != _hitState)
        {
            return;
        }

        _hitState.Complete();
    }

    /// <summary>
    /// HitState 종료 후 SwingState로 복귀
    /// </summary>
    private void HandleHitCompleted()
    {
        _handStateMachine.ChangeState(_swingState);

        Debug.Log("타격 애니메이션 완료: 공격 상태 복귀");
    }
    #endregion
}