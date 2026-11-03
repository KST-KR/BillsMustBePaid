using System;
using UnityEngine;

/// <summary>
/// 게임 전체 상태와 런의 시작 및 종료를 관리
/// 일시정지는 게임 상태와 별개의 겹침(overlay) 플래그로 관리
/// 일시정지 해제시 상태 유지와 이전화면으로 돌아가기
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 싱글톤
    public static GameManager Instance { get; private set; }
    #endregion

    #region 속성
    public EGameState State => _state;
    public bool IsRunActive => _isRunActive;

    /// <summary>
    /// 일시정지 중인지 여부 (게임 상태와는 별개)
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// 일시정지 가능 여부
    /// 플레이 중, 스테이지 클리어, 게임 오버 상태에서 가능
    /// </summary>
    public bool CanPause => !_isPaused && (_state == EGameState.Playing || _state == EGameState.StageClear || _state == EGameState.GameOver);
    #endregion

    #region 이벤트
    public event Action<EGameState> OnGameStateChanged;

    /// <summary>
    /// 일시정지의 시작과 헤재 때 발생 (true = 일시정지 시작)
    /// 게임 상태는 바뀌지 않으므로 OnGameStateChanged는 발생하지 않음.
    /// </summary>
    public event Action<bool> OnPauseChanged;

    public event Action OnRunStarted;
    public event Action OnRunEnded;
    #endregion

    #region 내부 변수
    private EGameState _state = EGameState.Title;
    private bool _isRunActive;
    private bool _isPaused;
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

        // 일시정지 중에 런이 종료되어도 시간이 멈춘채로 남지 않도록 해제
        SetPaused(false);

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

        // 상태가 실제로 바뀌면 일시정지를 해제
        // 어떤 경로든 시간 정지가 남지 않도록 한 곳에서 처리
        SetPaused(false);

        _state = nextState;

        Debug.Log("GameManager : ChangeGameState 게임 상태를 변경 이벤트를 발생");

        OnGameStateChanged?.Invoke(_state);
    }

    /// <summary>
    /// 게임을 일시정지, 상태는 바뀌지 않고 IsPaused만 true
    /// </summary>
    public void PauseGame()
    {
        if (!CanPause)
        {
            return;
        }

        Debug.Log("GameManager: PauseGame 게임을 일시정지");

        SetPaused(true);
    }

    /// <summary>
    /// 일시정지를 해제하고 게임 상태는 그대로 이므로 이전 화면으로 돌아감.
    /// </summary>
    public void ResumeGame()
    {
        if (!_isPaused)
        {
            return;
        }

        Debug.Log("GameManager: ResumeGame 일시정지 해제");

        SetPaused(false);
    }

    /// <summary>
    /// 일시정지 중이면 해제, 아니면 일시정지 (ESC 키 등에 연결)
    /// </summary>
    public void TogglePause()
    {
        if (_isPaused)
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

    /// <summary>
    /// 저장된 런의 활성 상태를 복원합니다.
    /// 게임 상태 변경 및 런 시작 이벤트는 발생시키지 않습니다.
    /// </summary>
    public void RestoreRunState()
    {
        _isRunActive = true;
        SetPaused(false);

        Debug.Log("GameManager : 저장된 런의 활성 상태를 복원");
    }
    #endregion

    #region 커스텀 (private)
    private void SetPaused(bool paused)
    {
        if (_isPaused == paused)
        {
            return;
        }

        _isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        OnPauseChanged?.Invoke(paused);
    }
    #endregion
}