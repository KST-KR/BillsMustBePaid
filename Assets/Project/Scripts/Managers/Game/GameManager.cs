using System;
using UnityEngine;

/// <summary>
/// 게임 전체의 진행 상태를 정의합니다.
/// </summary>
public enum EGameState
{
    Title,
    Playing,
    Paused,
    StageClear,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 외부에서 조회 가능
    public EGameState State => _state;
    public bool IsRunActive => _isRunActive;

    // 외부 시스템에서 구독 가능
    public event Action<EGameState> OnGameStateChanged;
    public event Action OnRunStarted;
    public event Action OnRunEnded;

    // 내부에서만 변경
    private EGameState _state = EGameState.Title;
    private bool _isRunActive;

    // 외부에서 호출 가능
    public void StartRun() { }
    public void EndRun() { }
    public void GameOver() { }

    // 변경할 상태를 매개변수로 전달
    public void ChangeGameState(EGameState nextState) { }
}