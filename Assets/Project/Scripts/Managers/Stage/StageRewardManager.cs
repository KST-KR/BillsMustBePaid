
using System;
using UnityEngine;

/// <summary>
/// 스테이지에서 획득한 골드를 기록하고,
/// 스테이지 클리어 시 보유 골드에 정산합니다.
/// </summary>
public class StageRewardManager : MonoBehaviour
{
    #region 싱글톤

    public static StageRewardManager Instance { get; private set; }

    #endregion

    #region 속성

    public int StageRewardGold => _stageRewardGold;
    public int StagePenaltyGold => _stagePenaltyGold;

    #endregion

    #region 이벤트

    public event Action<int> OnStageRewardChanged;
    public event Action<int, int> OnStageRewardSettled;

    #endregion

    #region 내부 변수

    private StageManager _stageManager;
    private CurrencyManager _currencyManager;

    private int _stageRewardGold;
    private int _stagePenaltyGold;
    private bool _isSettled;
    private bool _isSubscribed;

    #endregion

    #region 이벤트 함수

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 커스텀 메서드 (Private)

    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        _stageManager = StageManager.Instance;
        _currencyManager = CurrencyManager.Instance;

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted += HandleStageStarted;
            _stageManager.OnStageCleared += HandleStageCleared;
            _stageManager.OnStageFailed += HandleStageFailed;
        }

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
            _stageManager.OnStageCleared -= HandleStageCleared;
            _stageManager.OnStageFailed -= HandleStageFailed;
        }

        _stageManager = null;
        _currencyManager = null;
        _isSubscribed = false;
    }

    private void HandleStageStarted(int stageNumber)
    {
        _stageRewardGold = 0;
        _stagePenaltyGold = 0;
        _isSettled = false;

        OnStageRewardChanged?.Invoke(_stageRewardGold);
    }

    private void HandleStageCleared(int stageNumber)
    {
        SettleStageReward();
    }

    private void HandleStageFailed()
    {
        // 실패한 스테이지의 미정산 보상은 지급하지 않음.
        _stageRewardGold = 0;
        _stagePenaltyGold = 0;
        _isSettled = true;

        OnStageRewardChanged?.Invoke(_stageRewardGold);
    }

    private void SettleStageReward()
    {
        if (_isSettled)
        {
            return;
        }

        // 재화 관리자가 없으면 나중에 다시 정산할 수 있도록 유지
        if (_currencyManager == null)
        {
            _currencyManager = CurrencyManager.Instance;
        }

        if (_currencyManager == null)
        {
            Debug.LogError("CurrencyManager를 찾을 수 없어 보상을 정산하지 못했습니다.");
            return;
        }

        // 스테이지 보상 골드에서 연체 패널티를 계산하여 차감
        int sRewardGold = _stageRewardGold;
        _stagePenaltyGold = BillManager.Instance != null
            ? BillManager.Instance.CalculateOverdueIncomePenalty(sRewardGold)
            : 0;
        int rewardGold = Mathf.Max(0, sRewardGold - _stagePenaltyGold);

        if (rewardGold > 0 &&
            !_currencyManager.TryAdd(ECurrencyType.Gold, rewardGold))
        {
            Debug.LogError("스테이지 보상 골드 지급에 실패했습니다.");
            return;
        }

        // 지급 성공 후 정산 완료 처리
        _isSettled = true;

        int totalGold = _currencyManager.Gold;

        OnStageRewardSettled?.Invoke(rewardGold, totalGold);
    }

    #endregion

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 진행 중인 스테이지의 획득 골드를 기록
    /// </summary>
    public bool TryAddStageGold(int amount)
    {
        if (amount <= 0 || _isSettled)
        {
            return false;
        }

        if (_stageManager == null || !_stageManager.IsStageActive)
        {
            return false;
        }

        long newAmount = (long)_stageRewardGold + amount;

        if (newAmount > int.MaxValue)
        {
            return false;
        }

        _stageRewardGold = (int)newAmount;

        OnStageRewardChanged?.Invoke(_stageRewardGold);

        return true;
    }

    #endregion
}
