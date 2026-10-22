using System;
using UnityEngine;

/// <summary>
/// 게임 전체 상태와 런의 시작 및 종료를 관리합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 싱글톤

    public static GameManager Instance { get; private set; }

    #endregion

    #region 속성

    public EGameState State => _state;
    public bool IsRunActive => _isRunActive;

    #endregion

    #region 이벤트

    public event Action<EGameState> OnGameStateChanged;
    public event Action OnRunStarted;
    public event Action OnRunEnded;

    #endregion

    #region 내부 변수

    private EGameState _state = EGameState.Title;
    private bool _isRunActive;

    #endregion

    #region 이벤트 함수

    /// <summary>
    /// GameManager 싱글톤을 등록합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// GameManager가 파괴될 때 싱글톤 참조를 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 새로운 런을 시작합니다.
    /// </summary>
    public void StartRun()
    {
        if (_isRunActive)
        {
            return;
        }

        _isRunActive = true;

        ChangeGameState(EGameState.Playing);
        OnRunStarted?.Invoke();
    }

    /// <summary>
    /// 현재 런을 종료합니다.
    /// </summary>
    public void EndRun()
    {
        if (!_isRunActive)
        {
            return;
        }

        _isRunActive = false;
        OnRunEnded?.Invoke();
    }

    /// <summary>
    /// 게임 상태를 변경하고 변경 이벤트를 발생시킵니다.
    /// </summary>
    public void ChangeGameState(EGameState nextState)
    {
        if (_state == nextState)
        {
            return;
        }

        _state = nextState;
        OnGameStateChanged?.Invoke(_state);
    }

    /// <summary>
    /// 현재 런을 게임 오버 상태로 종료합니다.
    /// </summary>
    public void GameOver()
    {
        if (!_isRunActive)
        {
            return;
        }

        EndRun();
        ChangeGameState(EGameState.GameOver);
    }

    /// <summary>
    /// 게임을 완전히 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion
}   