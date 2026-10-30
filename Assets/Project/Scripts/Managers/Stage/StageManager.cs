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
    private PlayerStamina _playerStamina;

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

        if (_playerStamina == null)
        {
            _playerStamina = FindAnyObjectByType<PlayerStamina>();
        }
    }

    private void Start()
    {
        /// <summary>
        /// 스태미나에 따른 스테이지 실패 이벤트 구독과 해제
        /// </summary>
        if (_playerStamina != null)
        {
            _playerStamina.OnExhausted += HandleStaminaDepleted;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted += HandleRunStarted;
            GameManager.Instance.OnRunEnded += HandleRunEnded;
        }

        if (_playerStamina == null)
        {
            Debug.LogError("PlayerStamina가 없음.");
        }
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

        if (_playerStamina != null)
        {
            _playerStamina.OnExhausted -= HandleStaminaDepleted;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted -= HandleRunStarted;
            GameManager.Instance.OnRunEnded -= HandleRunEnded;
        }
    }
    #endregion

    #region 커스텀 메서드 (Private)
    private void HandleStaminaDepleted()
    {
        ClearStage();
    }

    private void HandleRunStarted()
    {
        ResetStage();
        // 런 시작 시 1스테이지 자동 시작
        StartStage(1); 
    }

    private void HandleRunEnded()
    {
        ResetStage();
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

        int previousStage = _currentStage;

        _currentStage = stageNumber;
        _isStageActive = true;

        GameManager.Instance.ChangeGameState(EGameState.Playing);

        if (previousStage != _currentStage)
        {
            OnStageChanged?.Invoke(_currentStage);
        }

        Debug.Log($"StartStage : 지정한 번호의 스테이지를 시작합니다. / 스테이지 넘버 : {_currentStage}");

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

        Debug.Log("ClearStage : 현재 스테이지를 클리어 처리합니다.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(EGameState.StageClear);
        }

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

        Debug.Log("NextStage : 다음 스테이지로 전환합니다.");

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

        Debug.Log("FailStage : 현재 스테이지의 실패를 처리합니다.");

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
        bool stageChanged = _currentStage != 0;

        _currentStage = 0;
        _isStageActive = false;

        Debug.Log("ResetStage : 스테이지 진행 정보를 초기화 합니다.");

        if (stageChanged)
        {
            OnStageChanged?.Invoke(_currentStage);
        }

        // 활성 스테이지를 초기화한 경우 실패나 클리어 이벤트는 발생시키지 않음.
        // ResetStage는 진행 정보 초기화만 담당
    }

    /// <summary>
    /// 저장된 스테이지 결과 화면 상태를 복원
    /// 실제 스테이지 시작 및 클리어 이벤트는 발생시키지 않음.
    /// </summary>
    public void RestoreStageClear(int stageNumber)
    {
        if (stageNumber <= 0)
        {
            Debug.LogError("복원할 스테이지 번호가 잘못됨.");
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsRunActive)
        {
            Debug.LogError("활성화된 런이 없어 스테이지를 복원할 수 없음.");
            return;
        }

        int previousStage = _currentStage;

        _currentStage = stageNumber;
        _isStageActive = false;

        if (previousStage != _currentStage)
        {
            OnStageChanged?.Invoke(_currentStage);
        }

        GameManager.Instance.ChangeGameState(EGameState.StageClear);

        Debug.Log($"StageManager : 저장된 스테이지 결과 화면 복원 / 스테이지 번호 : {_currentStage}");
    }

    /// <summary>
    /// 스테이지를 시작하지 않고 저장된 스테이지 번호만 복원
    /// </summary>
    /// <param name="stageNumber"></param>
    public void RestoreStageProgress(int stageNumber)
    {
        if (stageNumber <= 0)
        {
            Debug.LogWarning("복원할 스테이지 번호가 올바르지 않음.");
            return;
        }

        bool hasChanged = _currentStage != stageNumber;

        _currentStage = stageNumber;

        // 스테이지는 아직 진행중이 아님.
        _isStageActive = false;

        if (hasChanged)
        {
            OnStageChanged?.Invoke(_currentStage);
        }

        Debug.Log($"스테이지 진행 정보 복원: {_currentStage}");
    }
    #endregion
}
