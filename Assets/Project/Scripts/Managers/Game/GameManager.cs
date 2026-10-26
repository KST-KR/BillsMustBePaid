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
    public bool CanPause => _isRunActive && _state == EGameState.Playing;
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
    /// GameManager 싱글톤을 등록
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
    /// GameManager가 파괴될 때 싱글톤 참조를 해제
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 일시 정지 중 씬이 바뀌어도 시간이 멈춘채로 남지 않도록 복구
            Time.timeScale = 1f;
            Instance = null;
        }
    }
    #endregion

    #region 커스텀 메서드 (Public)
    /// <summary>
    /// 새로운 런을 시작
    /// </summary>
    public void StartRun()
    {
        if (_isRunActive)
        {
            return;
        }

        _isRunActive = true;

        Debug.Log("GameManager : StartRun 새로운 런을 시작");

        ChangeGameState(EGameState.Playing);
        OnRunStarted?.Invoke();
    }

    /// <summary>
    /// 현재 런을 종료
    /// </summary>
    public void EndRun()
    {
        if (!_isRunActive)
        {
            return;
        }

        _isRunActive = false;

        // 일시정지 중에 런이 종료되어도 시간이 멈춘채로 남지 않도록 복구
        Time.timeScale = 1f;

        Debug.Log("GameManager : EndRun 현재 런을 종료");

        OnRunEnded?.Invoke();
    }

    /// <summary>
    /// 게임 상태를 변경하고 변경 이벤트를 발생시킴.
    /// </summary>
    public void ChangeGameState(EGameState nextState)
    {
        if (_state == nextState)
        {
            return;
        }

        // 일시정지에서 벗어나는 모든 경로에서 시간 정지를 해제
        if (_state == EGameState.Paused && nextState != EGameState.Paused)
        {
            Time.timeScale = 1f;
        }

        _state = nextState;

        Debug.Log("GameManager : ChangeGameState 게임 상태를 변경 이벤트를 발생");

        OnGameStateChanged?.Invoke(_state);
    }

    public void PauseGame()
    {
        if (!CanPause)
        {
            return;
        }

        // 상태 변경 이벤트를 받는 쪽이 이미 멈춘 시간을 기준으로 동작하도록 먼저 설정
        Time.timeScale = 0f;

        Debug.Log("GameManager: PauseGame 게임을 일시정지");

        ChangeGameState(EGameState.Paused);
    }

    /// <summary>
    /// 일시정지를 해제하고 Playing 상태로 돌아감.
    /// </summary>
    public void ResumeGame()
    {
        if (_state != EGameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;

        Debug.Log("GameManager: ResumeGame 일시정지 해제");

        ChangeGameState(EGameState.Playing);
    }

    /// <summary>
    /// 일시정지 중이면 해제, Playing이면 일시정지 (ESC 키 등에 연결)
    /// </summary>
    public void TogglePause()
    {
        if (_state == EGameState.Paused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// 현재 런을 종료하고 타이틀 상태로 돌아감 (일시정지 메뉴의 메인 메뉴 이동)
    /// 타이틀 씬으로의 전환은 호출하는 쪽에서 처리
    /// </summary>
    public void ReturnToTitle()
    {
        EndRun();
        ChangeGameState(EGameState.Title);
    }

    /// <summary>
    /// 현재 런을 게임 오버 상태로 종료
    /// </summary>
    public void GameOver()
    {
        if (!_isRunActive)
        {
            return;
        }

        Debug.Log("GameManager : GameOver 현재 런을 게임 오버 상태로 종료합니다.");

        EndRun();
        ChangeGameState(EGameState.GameOver);
    }

    /// <summary>
    /// 게임을 완전히 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("GameManager : QuitGame 게임을 완전히 종료합니다.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
#endregion
}   