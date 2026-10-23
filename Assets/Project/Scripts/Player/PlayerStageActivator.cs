using UnityEngine;

/// <summary>
/// 스테이지가 진행 중일 때만 플레이어를 보이게 하고, 플레이 관련 컴포넌트를 동작
/// 스테이지 클리어, 게임 오버, 타이틀 상태에선느 플레이어가 보이지 않고 이동/공격 X
/// 게임 상태(EGameState)와 스테이지 활성 여부를 기준으로 판단
/// </summary>
public class PlayerStageActivator : MonoBehaviour
{
    #region 인스펙터
    [Header("표시 대상")]
    [Tooltip("화면에 보이는 오브젝트")]
    [SerializeField] private GameObject _visualRoot;

    [Header("스테이지 진행 중에만 켜 둘 컴포넌트")]
    [Tooltip("클리어/게임 오버/타이틀에서는 꺼지고 다음 스테이지 시작시 다시 켜짐.")]
    [SerializeField] private Behaviour[] _gameplayBehavers;
    #endregion

    #region 내부 변수
    // 구독 시점의 참조를 보관하여 파괴순서와 상관없이 구독 해제
    private GameManager _gameManager;
    private StageManager _stageManager;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 플레이어가 화면에 보이는 상태 (Playing 또는 Paused이며 스테이지 진행 중)
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// 플레이어가 행동을 할 수 있는 상태 (Playing이며 스테이지 진행 중)
    /// 이동/공격/판정 스크립트에서 이 값을 확인하면 됨.
    /// </summary>
    public bool CanAct { get; private set; }
    #endregion

    #region 이벤트 함수
    /// <summary>
    /// 다른 매니저의 Awake가 끝난 뒤인 Start에서 구독하고 현재 상태 적요
    /// </summary>
    void Start()
    {
        ValidateTargets();

        _gameManager = GameManager.Instance;
        _stageManager = StageManager.Instance;

        if (_gameManager != null)
        {
            _gameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted += HandleStageStarted;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChanged -= HandleGameStateChanged;
            _gameManager = null;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
            _stageManager = null;
        }
    }
    #endregion

    #region 커스텀 (private)
    private void HandleGameStateChanged(EGameState state)
    {
        Refresh();
    }

    private void HandleStageStarted(int stageNumber)
    {
        Refresh();
    }

    /// <summary>
    /// 현재 게임 상태와 스테이지 진행 여부를 읽어서 표시와 동작을 적용
    /// </summary>
    private void Refresh()
    {
        bool stageActive = _stageManager != null && _stageManager.IsStageActive;
        EGameState state = _gameManager != null ? _gameManager.State : EGameState.Title;

        IsVisible = stageActive && (state == EGameState.Playing || state == EGameState.Paused);
        CanAct = stageActive && state == EGameState.Playing;

        if (_visualRoot != null)
        {
            _visualRoot.SetActive(IsVisible);
        }

        // 컴포넌트는 스테이지 진행중에만 켜두기
        // 일시정지 중의 정지는 각 스트립트가 CanAct를 확인해서 처리
        // (Pause에서 끄면 OnDisable로 공격진행과 쿨타임이 초기화되기 때문
        SetGamePlayEnabled(IsVisible);
    }

    private void SetGamePlayEnabled(bool isEnabled)
    {
        if (_gameplayBehavers == null)
        {
            return;
        }

        for (int i = 0; i < _gameplayBehavers.Length; i++)
        {
            Behaviour behaviour = _gameplayBehavers[i];

            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = isEnabled;
            }
        }
    }

    /// <summary>
    /// 잘못 연결하면 이벤트 구독이 끊기거나 UI 갱신이 멈추는 대상을 걸러냄.
    /// </summary>
    private void ValidateTargets()
    {
        if (_visualRoot == gameObject)
        {
            Debug.LogError("루트 오브젝트를 숨기면 스크립트가 멈추므로 자식 오브젝트를 지정해야 함.");
            _visualRoot = null;
        }

        if (_gameplayBehavers == null)
        {
            return;
        }

        for (int i = 0; i<_gameplayBehavers.Length; i++)
        {
            if (_gameplayBehavers[i] is PlayerStats || _gameplayBehavers[i] is PlayerStamina)
            {
                Debug.LogWarning($"{_gameplayBehavers[i].GetType().Name}은 목록에서 빼야 함.");
                _gameplayBehavers[i] = null;
            }
        }
    }
    #endregion

}
