using System;
using UnityEngine;

/// <summary>
/// 손의 공격 진행 상태를 관리
/// </summary>
public class SwingState : IHandState
{
    #region 내부 변수
    private readonly Func<float> _getHitTime;
    private readonly Func<float> _getAttackDuration;
    private readonly Func<int> _performAttack;

    private readonly Action _onAttackStarted;
    private readonly Action _onAttackEnded;
    private readonly Action<int> _onAttackPerformed;

    private float _attackElapsed;

    private bool _hasHit;
    private bool _isCompleted;
    #endregion

    #region 프로퍼티
    public float AttackElapsed => _attackElapsed;
    public bool HasHit => _hasHit;
    #endregion

    #region 생성자
    public SwingState(
        Func<float> getHitTime,
        Func<float> getAttackDuration,
        Func<int> performAttack,
        Action onAttackStarted,
        Action onAttackEnded,
        Action<int> onAttackPerformed)
    {
        _getHitTime = getHitTime;
        _getAttackDuration = getAttackDuration;
        _performAttack = performAttack;

        _onAttackStarted = onAttackStarted;
        _onAttackEnded = onAttackEnded;
        _onAttackPerformed = onAttackPerformed;
    }
    #endregion

    #region 상태 함수
    public void Enter()
    {
        // 상태에 다시 진입해도 기존 공격 진행 데이터는 유지
    }

    public void Update()
    {
        if (_isCompleted)
        {
            return;
        }

        _attackElapsed += Time.deltaTime;

        float hitTime = _getHitTime();
        float attackDuration = _getAttackDuration();

        // 타격 시점에 한번만 공격 판정
        if (!_hasHit && _attackElapsed >= hitTime)
        {
            _hasHit = true;

            int hitCount = _performAttack();

            _onAttackPerformed?.Invoke(hitCount);

            // 공격 성공시 HitState로 전환될 수 있으므로 현재 공격 업데이트를 여기서 마무리
            if (hitCount > 0)
            {
                return;
            }
        }

        // 공격 시간이 끝나면 공격 종료
        if (_attackElapsed >= attackDuration)
        {
            _isCompleted = true;
            _onAttackEnded?.Invoke();
        }
    }

    public void Exit()
    {
        // 공격 진행 데이터 유지
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 새로운 공격을 시작
    /// 기존 공격 재개와 구분하기 위해서만 초기화
    /// </summary>
    public void StartAttack()
    {
        _attackElapsed = 0f;
        _hasHit = false;
        _isCompleted = false;

        _onAttackStarted?.Invoke();
    }

    /// <summary>
    /// 공격 중단
    /// </summary>
    public void CancelAttack()
    {
        _attackElapsed = 0f;
        _hasHit = false;
        _isCompleted = false;
    }
    #endregion
}
