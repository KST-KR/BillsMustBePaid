using System;

/// <summary>
/// 스태미나가 고갈되었을 때 손의 행동을 제한하는 상태
/// </summary>
public class TiredState : IHandState
{
	#region 내부 변수
	private readonly Func<bool> _isStaminaExhausted;
	private readonly Action _onRecovered;
	#endregion

	#region 생성자
	public TiredState(Func<bool> isStaminaExhausted, Action onRecovered)
	{
		_isStaminaExhausted = isStaminaExhausted;
		_onRecovered = onRecovered;
	}
	#endregion

	public void Enter()
	{
		// 피로 상태 진입
	}

	public void Update()
	{
		// 다음 스테이지에서 스태미나 고갈이 해제되었는지 확인
		if (!_isStaminaExhausted())
		{
			_onRecovered?.Invoke();
		}
	}

	public void Exit()
	{
		// 피로 상태 종료
	}

}
