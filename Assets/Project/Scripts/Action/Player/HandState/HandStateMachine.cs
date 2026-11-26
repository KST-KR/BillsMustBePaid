using UnityEngine;

/// <summary>
/// 손의 현재 상태를 관리하고 상태 전환을 처리
/// </summary>
public class HandStateMachine
{
    #region 프로퍼티
    public IHandState CurrentState { get; private set; }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 새로운 상태로 전환
    /// </summary>
    /// <param name="nextState"></param>
    public void ChangeState(IHandState nextState)
    {
        if (nextState == null)
        {
            Debug.LogWarning("전환할 상태가 없음.");
            return;
        }

        // 현재 상태와 전환할 상태가 같다면 전환하지 않음.
        if (ReferenceEquals(CurrentState, nextState))
        {
            return;
        }

        // 기존 상태 종료
        CurrentState?.Exit();

        // 새로운 상태 등록
        CurrentState = nextState;

        // 새로운 상태 시작
        CurrentState.Enter();
    }

    /// <summary>
    /// 현재 상태의 Update를 호출
    /// </summary>
    public void Update()
    {
        CurrentState?.Update();
    }
    #endregion

}
