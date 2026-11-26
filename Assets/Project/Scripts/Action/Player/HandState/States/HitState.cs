using System;

/// <summary>
/// 공격이 성공했을 때 손의 피격 반응을 관리
/// </summary>
public class HitState : IHandState
{
    #region 내부 변수
    private readonly Action _onHit;
    private readonly Action _onCompleted;
    #endregion

    #region 생성자
    public HitState(Action onHit, Action onCompleted)
    {
        _onHit = onHit;
        _onCompleted = onCompleted;
    }
    #endregion

    public void Enter()
    {
        // 공격 성공에 따른 피격 반응 실행
        _onHit?.Invoke();
    }

    public void Update()
    {
        // 피격 애니메이션이 끝나면 Complete()를 호출하도록 구성
    }

    public void Exit()
    {
        // 피격 상태 종료
    }

    /// <summary>
    /// 피격 반응이 끝났을 때 호출
    /// </summary>
    public void Complete()
    {
        _onCompleted?.Invoke();
    }

}
