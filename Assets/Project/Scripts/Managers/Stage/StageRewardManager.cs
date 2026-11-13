using System;
using UnityEngine;
using static CoinObjectPoolManager;

[Serializable]
public struct CrashLevelInfo
{
    [Tooltip("다음 레벨로 넘어가기 위해 잡아야 하는 돼지 저금통 횟수")]
    public int destroyCount;

    [Tooltip("레벨에서 추가로 지급 되는 골드")]
    public int bonusReward;
}

/// <summary>
/// 스테이지에서 획득한 골드를 기록하고,
/// 스테이지 클리어 시 보유 골드에 정산합니다.
/// </summary>
public class StageRewardManager : Singleton<StageRewardManager>
{
    #region 속성

    public int StageRewardGold => _stageRewardGold;
    public int StagePenaltyGold => _stagePenaltyGold;

    public int AccuracyBonusGold => _accuracyBonusGold;
    public int StageRewardBeforePenaltyGold => _stageRewardGold + _accuracyBonusGold;

    public int CurrentCrashLevel => _currentCrashLevel;
    #endregion

    #region 이벤트

    public event Action<int> OnStageRewardChanged;
    public event Action<int, int> OnStageRewardSettled;

    #endregion

    #region 인스펙터
    [Header("파괴 레벨 시스템")]
    [SerializeField] private int _maxLevel = 5;

    [Header("레벨별 데이터 설정 (인스펙터에서 수정 가능)")]
    [SerializeField] private CrashLevelInfo[] _levelTable;

    [Header("플레이어 정확도")]
    [SerializeField] private PlayerAccuracy _playerAccuracy;
    #endregion

    #region 내부 변수

    private StageManager _stageManager;
    private CurrencyManager _currencyManager;
    private PerkManager _perkManager;

    private int _stageRewardGold;
    private int _stagePenaltyGold;
    private int _currentCrashLevel = 1;
    private int _totalCrashNumber = 0;
    private int _accuracyBonusGold;

    private bool _isSettled;
    private bool _isSubscribed;

    public int EarnedSilverCoinCount { get; private set; }
    public int EarnedGoldCoinCount { get; private set; }
    #endregion

    #region 이벤트 함수

    private void Start()
    {
        if (!IsActiveInstance)
        {
            return;
        }

        SubscribeEvents();
    }

    protected override void OnSingletonDestroy()
    {
        UnsubscribeEvents();
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
        _perkManager = PerkManager.Instance;

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
        _accuracyBonusGold = 0;
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
        _accuracyBonusGold = 0;
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

        // 1. 기본 스테이지 보상
        int baseRewardGold = _stageRewardGold;
        if (_perkManager == null)
        {
            _perkManager = PerkManager.Instance;
        }
        if (_perkManager != null)
        {
            float rewardRate = Mathf.Max(0.0f, _perkManager.GetTotalPercent(EPerkTargetStat.Reward));
            float rewardFlat = Mathf.Max(0.0f, _perkManager.GetTotalFlat(EPerkTargetStat.Reward));
            baseRewardGold = Mathf.Max(0, Mathf.RoundToInt((baseRewardGold + rewardFlat) * (1.0f + rewardRate)));
        }

        // 2. 정확도에 비례해 기본 보상의 최대 10%까지 보너스 지급
        float accuracy = _playerAccuracy != null ? _playerAccuracy.Accuracy : 100f;

        _accuracyBonusGold = Mathf.FloorToInt(baseRewardGold * accuracy / 1000f);

        // 기본 보상 + 정확도 보너스
        int rewardBeforePenalty = baseRewardGold + _accuracyBonusGold;

        // 3. 정확도가 반영된 보상을 기준으로 연체 패널티 계산
        _stagePenaltyGold = BillManager.Instance != null ? BillManager.Instance.CalculateOverdueIncomePenalty(rewardBeforePenalty) : 0;

        // 4. 패널티 차감 후 최종 보상 계산
        int rewardGold = Mathf.Max(0, rewardBeforePenalty - _stagePenaltyGold);

        if (rewardGold > 0 && !_currencyManager.TryAdd(ECurrencyType.Gold, rewardGold))
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
    /// 스테이지 시작 시 획득 코인 개수 초기화
    /// </summary>
    public void ResetEarnedCoinCounts()
    {
        EarnedSilverCoinCount = 0;
        EarnedGoldCoinCount = 0;
    }

    /// <summary>
    /// 코인이 카메라로 흡수될 때 타입별 획득 개수 증가
    /// </summary>
    public void AddEarnedCoinCount(CoinObjectPoolManager.CoinType coinType, int amount = 1)
    {
        switch (coinType)
        {
            case CoinObjectPoolManager.CoinType.SILVER:
                EarnedSilverCoinCount += amount;
                break;

            case CoinObjectPoolManager.CoinType.GOLD:
                EarnedGoldCoinCount += amount;
                break;
        }
    }

    /// <summary>
    /// 돼지 저금통이 파괴될 때 호출되어 누적 횟수와 레벨업을 관리
    /// </summary>
    public void NotifyPigBankDestroyed()
    {
        _totalCrashNumber++;
        Debug.Log($"StageReward : 돼지 파괴 누적 횟수: {_totalCrashNumber}");

        int currentIndex = _currentCrashLevel - 1;

        // 배열 범위를 벗어나지 않는지 확인
        if (currentIndex < _levelTable.Length)
        {
            // 현재 레벨에 설정된 목표 횟수에 도달했는지 확인
            if (_totalCrashNumber >= _levelTable[currentIndex].destroyCount)
            {
                _totalCrashNumber = 0;

                if (_currentCrashLevel < _maxLevel)
                {
                    _currentCrashLevel++;
                    Debug.Log($"★★ LevelUp  ★★ : 파괴 레벨 상승! 현재 레벨: {_currentCrashLevel}");
                }
            }
        }
    }

    /// <summary>
    /// 현재 레벨에 해당하는 추가 보상을 반환
    /// </summary>
    public int GetCurrentLevelBonusReward()
    {
        int currentIndex = _currentCrashLevel - 1;
        if (currentIndex >= 0 && currentIndex < _levelTable.Length)
        {
            return _levelTable[currentIndex].bonusReward;
        }
        return 0; // 예외 처리
    }

    /// <summary>
    /// 게임 오버(파산) 시 파괴 레벨 시스템을 초기화
    /// </summary>
    public void ResetCrashLevelSystem()
    {
        _currentCrashLevel = 1;
        _totalCrashNumber = 0;
        Debug.Log("StageReward : 파산/게임 오버로 인해 파괴 레벨이 초기화되었습니다.");
    }

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