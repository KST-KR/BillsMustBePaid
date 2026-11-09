using System;
using UnityEngine;

/// <summary>
/// 스테이지의 시작, 클리어 및 전환을 관리
/// 게임 전체 상태는 GameManager에서 관리
/// </summary>
public class StageManager : Singleton<StageManager>
{
    #region 인스펙터
    [Header("스테이지 환경")]
    [SerializeField] private TableCrackEffect _tableCrackEffect;
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
    private GameManager _gameManager;

    private int _currentStage;
    private bool _isStageActive;
    #endregion

    #region 이벤트 함수

    protected override void OnSingletonAwake()
    {
        if (_playerStamina == null)
        {
            _playerStamina = FindAnyObjectByType<PlayerStamina>();
        }
    }

    private void Start()
    {
        if (!IsActiveInstance)
        {
            return;
        }

        /// <summary>
        /// 스태미나에 따른 스테이지 실패 이벤트 구독과 해제
        /// </summary>
        if (_playerStamina != null)
        {
            _playerStamina.OnExhausted += HandleStaminaDepleted;
        }

        _gameManager = GameManager.Instance;

        if (_gameManager != null)
        {
            _gameManager.OnRunStarted += HandleRunStarted;
            _gameManager.OnRunEnded += HandleRunEnded;
        }

        if (_playerStamina == null)
        {
            Debug.LogError("PlayerStamina가 없음.");
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (_playerStamina != null)
        {
            _playerStamina.OnExhausted -= HandleStaminaDepleted;
        }

        if (_gameManager != null)
        {
            _gameManager.OnRunStarted -= HandleRunStarted;
            _gameManager.OnRunEnded -= HandleRunEnded;
        }

        _playerStamina = null;
        _gameManager = null;
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

    /// <summary>
    /// 책상에 생성된 균열을 모두 제거
    /// </summary>
    private void ClearTableCracks()
    {
        if (_tableCrackEffect == null)
        {
            Debug.LogWarning("TableCrackEffect가 연결되지 않음.", this);
            return;
        }

        _tableCrackEffect.ClearCracks();
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

        // 새로운 스테이지 시작 전 기존 균열 제거
        ClearTableCracks();

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

        // 스테이지 진행 정보 초기화
        _currentStage = 0;
        _isStageActive = false;

        // 기존 균열 제거
        ClearTableCracks();

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