using System;
using UnityEngine;

/// <summary>
/// 스테이지의 시작, 클리어 및 전환을 관리합니다.
/// 게임 전체 상태는 GameManager에서 관리합니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    #region 싱글톤

    public static StageManager Instance { get; private set; }

    #endregion

    #region 속성

    public int CurrentStage => _currentStage;
    public bool IsStageActive => _isStageActive;

    #endregion

    #region 이벤트

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action<int> OnStageChanged;
    public event Action OnStageFailed;

    #endregion

    #region 내부 변수

    private int _currentStage;
    private bool _isStageActive;

    #endregion

    #region 이벤트 함수

    /// <summary>
    /// StageManager 싱글톤을 등록합니다.
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
    /// StageManager가 파괴될 때 싱글톤 참조를 해제합니다.
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
    /// 지정한 번호의 스테이지를 시작합니다.
    /// </summary>
    public void StartStage(int stageNumber)
    {
        if (stageNumber <= 0 || _isStageActive)
        {
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsRunActive)
        {
            return;
        }

        int previouseStage = _currentStage;

        _currentStage = stageNumber;
        _isStageActive = true;

        GameManager.Instance.ChangeGameState(EGameState.Playing);

        if (previouseStage != _currentStage)
        {
            OnStageChanged?.Invoke(_currentStage);
        }

        OnStageStarted?.Invoke(_currentStage);
    }

    /// <summary>
    /// 현재 스테이지를 클리어 처리합니다.
    /// </summary>
    public void ClearStage()
    {
        if (!_isStageActive)
        {
            return;
        }

        _isStageActive = false;

        GameManager.Instance.ChangeGameState(EGameState.StageClear);

        OnStageCleared?.Invoke(_currentStage);
    }

    /// <summary>
    /// 다음 스테이지로 전환합니다.
    /// </summary>
    public void NextStage()
    {
        if (_isStageActive || _currentStage == int.MaxValue)
        {
            return;
        }

        StartStage(_currentStage + 1);
    }

    /// <summary>
    /// 현재 스테이지의 실패를 처리합니다.
    /// </summary>
    public void FailStage()
    {
        if (!_isStageActive)
        {
            return;
        }

        _isStageActive = false;

        OnStageFailed?.Invoke();

        // 현재 게임 규칙에서는 스테이지 실패를 게임 오버로 처리
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    /// <summary>
    /// 스테이지 진행 정보를 초기화합니다.
    /// </summary>
    public void ResetStage()
    {
        bool wasStageActive = _isStageActive;
        bool stageChanged = _currentStage != 0;

        _currentStage = 0;
        _isStageActive = false;

        if (stageChanged)
        {
            OnStageChanged?.Invoke(_currentStage);
        }

        // 활성 스테이지를 초기화한 경우 실패나 클리어 이벤트는 발생시키지 않음.
        // ResetStage는 진행 정보 초기화만 담당
    }

    #endregion
}

