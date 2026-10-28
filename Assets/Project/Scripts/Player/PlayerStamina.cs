using System;
using UnityEngine;

/// <summary>
/// [PL-STAMINA-01 ~ 09] 플레이어 스태미나 관리
/// 스테이지 시작 시 초기화하고, 진행시간에 따라 소모하며, 고갈 이벤트 발생
/// 최대 스태미나는 PlayerStats이 계산한 값 사용
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    [SerializeField] private PlayerStats _playerStats;

    [Header("스태미나 설정")]
    [Tooltip("스테이지 시작시 현재 스태미나 = 최대 스태미나")]
    [SerializeField, Range(0.01f, 1f)] private float _initialRatio = 1f;

    [Tooltip("스테이지 진행 중 초당 소모량")]
    [SerializeField, Min(0f)] private float _drainPerSecond = 5f;
    #endregion

    #region 내부 변수
    private float _current;
    private float _lastMax;
    private bool _isExhausted;
    
    // 구독 시점의 참조를 보관하여, 파괴 순서와 상관없이 구독 해제
    private StageManager _stageManager;
    private GameManager _gameManager;
    #endregion

    #region 프로퍼티
    public float Current => _current;

    public float Max => _playerStats != null ? _playerStats.MaxStamina : 0f;

    public bool IsExhausted => _isExhausted;
    #endregion

    #region 이벤트
    /// <summary>
    /// [PL-STAMINA-06] 현재 스태미나 또는 최대 스태미나가 변경되면 (현재, 최대) 전달
    /// </summary>
    public event Action<float, float> OnStaminaChanged;

    /// <summary>
    /// [PL-STAMINA-07] 스태미나가 0에 도달하면 고갈 상태로 전환하며 한 번만 발생
    /// </summary>
    public event Action OnExhausted;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (_playerStats == null)
        {
            _playerStats = GetComponent<PlayerStats>();
        }
    }

    /// <summary>
    /// 다른 매니저의 Awake가 끝난 뒤인 Start에서 구독
    /// </summary>
    private void Start()
    {
        if (_playerStats == null)
        {
            Debug.LogError("[PlayerStamina] PlayerStats가 없어 스태미나를 사용할 수 없음");
            enabled = false;
            return;
        }

        _stageManager = StageManager.Instance;
        _gameManager = GameManager.Instance;

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted += HandleStageStarted;
        }

        // [PL-STAMINA-05] 최대 스태미나가 바뀌면 현재 값을 조정
        _playerStats.OnStateChanged += HandleStatsChanged;

        // 스테이지 시작 이벤트보다 늦게 생성돼도 초기 상태를 갖도록 한 번 초기화
        ResetForStage();
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
            _stageManager = null;
        }

        if (_playerStats != null)
        {
            _playerStats.OnStateChanged -= HandleStatsChanged;
        }

        _gameManager = null;
    }

    /// <summary>
    /// [PL-STAMINA-02] 스테이지 진행 중에만 시간에 따라 스태미나를 감소
    /// </summary>
    private void Update()
    {
        if (!CanDrain())
        {
            return;
        }

        SetCurrent(_current - _drainPerSecond * Time.deltaTime);
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// [PL-STAMINA-02] 초당 소모량 설정
    /// </summary>
    public void SetDrainPerSecond(float amountPerSecond)
    {
        _drainPerSecond = Mathf.Max(0f, amountPerSecond);
    }

    /// <summary>
    /// [PL-STAMINA-03] 지정한 양을 소모. 현재 잔량보다 많이 소모하지 않으며 실제 소모량을 반환
    /// </summary>
    public float Consume(float amount)
    {
        if (amount < 0f || _isExhausted)
        {
            return 0f;
        }

        float consumed = Mathf.Min(amount, _current);
        SetCurrent(_current - consumed);
        return consumed;
    }

    /// <summary>
    /// [PL-STAMINA-04] 스태미나를 회복. 최대치를 넘지 않으며 실제 회복량을 반환
    /// [PL-STAMINA-08] 고갈 이후에는 회복해도 되살아나지 않음
    /// </summary>
    public float Recover(float amount)
    {
        if (amount <= 0f || _isExhausted)
        {
            return 0f;
        }

        float before = _current;
        SetCurrent(_current + amount);
        return _current - before;
    }

    /// <summary>
    /// 저금통 파괴 보상 등 최대 스태미나 초과를 허용하는 회복
    /// 고갈 상태에서는 회복하지 못함.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public float RecoverBeyondMax(float amount)
    {
        if (amount <= 0f || _isExhausted)
        {
            return 0f;
        }

        float before = _current;

        SetCurrent(_current + amount);

        return _current - before;
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// [PL-STAMINA-01] 스테이지 시작 시 현재 스태미나를 초기화하고 고갈 상태를 해제
    /// [PL-STAMINA-08] 고갈 상태는 새 스테이지(재시작)에서만 초기화됨
    /// </summary>
    private void ResetForStage()
    {
        float max = Max;

        _isExhausted = false;
        _lastMax = max;
        _current = Mathf.Clamp(max * _initialRatio, 0f, max);

        NotifyChanged();
    }

    private void HandleStageStarted(int stageNumber)
    {
        ResetForStage();
    }

    /// <summary>
    /// [PL-STAMINA-05] 최대 스태미나 변경 반영
    /// 최대 스태미나가 변경되어도 현재 스태미나를 강제로 감소시키지 않음.
    /// 최대 스태미나 변경 시 UI 갱신 이벤트를 전달.
    /// </summary>
    private void HandleStatsChanged()
    {
        float max = Max;
        bool maxChanged = !Mathf.Approximately(_lastMax, max);
        _lastMax = max;

        if (maxChanged)
        {
            NotifyChanged();
        }
    }

    /// <summary>
    /// 스테이지가 진행 중이고 게임이 Playing 상태일 때만 소모
    /// </summary>
    /// <returns></returns>
    private bool CanDrain()
    {
        return !_isExhausted && _drainPerSecond > 0f
            && _gameManager != null && _gameManager.State == EGameState.Playing
            && _stageManager != null && _stageManager.IsStageActive
            && !_gameManager.IsPaused;
    }

    /// <summary>
    /// 현재 스태미나를 0 이상으로 설정하고 변경 이벤트 발생.
    /// 최대 스태미나 초과를 허용.
    /// 0에 도달하면 고갈 처리
    /// </summary>
    /// <param name="value"></param>
    private void SetCurrent(float value)
    {
        float clamped = Mathf.Max(0f, value);

        if (Mathf.Approximately(_current, clamped))
        {
            return;
        }

        _current = clamped;
        NotifyChanged();

        if (_current <= 0f && !_isExhausted)
        {
            Exhaust();
        }
    }

    private void Exhaust()
    {
        _isExhausted = true;
        OnExhausted?.Invoke();
    }

    /// <summary>
    /// 이벤트 전달
    /// </summary>
    private void NotifyChanged()
    {
        OnStaminaChanged?.Invoke(_current, Max);
    }
    #endregion

    #region 테스트
    [ContextMenu("테스트: 10 소모")]
    private void TestConsume() => Consume(10f);

    [ContextMenu("테스트: 10 회복")]
    private void TestRecover() => Recover(10f);

    [ContextMenu("테스트: 즉시 고갈")]
    private void TestExhaust() => Consume(float.MaxValue);
    #endregion
}
