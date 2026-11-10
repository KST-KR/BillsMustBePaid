using System;

/// <summary>
/// 손이 대기 중일 때 공격 가능 여부를 확인하는 상태
/// </summary>
public class IdleState : IHandState
{
    #region 내부 변수
    // Func<bool>: 호출하면 bool 값을 반환하는 메서드를 전달받음.
    private readonly Func<bool> _canStartAttack;
    // Action: 호출하면 특정 동작을 실행하는 메서드를 전달받음.
    private readonly Action _beginAttack;
    #endregion

    #region 생성자
    public IdleState(Func<bool> canStartAttack, Action beginAttack)
    {
        _canStartAttack = canStartAttack;
        _beginAttack = beginAttack;
    }
    #endregion

    #region 상태 함수
    public void Enter()
    {
        // 대기 상태 진입
    }

    public void Update()
    {
        // 공격을 시작할 수 없다면 대기
        if (!_canStartAttack())
        {
            return;
        }

        // 공격을 시작할 수 있다면 시작
        _beginAttack();
    }

    public void Exit()
    {
        // 대기 상태 종료
    }
    #endregion

}
