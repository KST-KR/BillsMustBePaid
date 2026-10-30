using System;
using System.Collections.Generic;
using UnityEngine;

public enum ECurrencyTransactionType
{
    Unspecified,
    Purchase,
    Upgrade,
    BillPayment,
    Other
}

/// <summary>
/// [FIN-01-01][FIN-01-02][FIN-01-03][FIN-02-02][FIN-03-01] 재화 잔고와 거래 관리
/// 공통 재화 증감과 잔고 알림 제공
/// 거래 유형별 지출 검증과 결과 알림 제공
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    #region 싱글톤
    public static CurrencyManager Instance { get; private set; }
    #endregion

    #region 속성
    public int Gold => _gold;
    public int LegacyPoints => _legacyPoints;
    #endregion

    #region 이벤트
    public event Action<int> OnAmountChanged;
    public event Action<ECurrencyType, int, int> OnBalanceChanged;
    public event Action<string, ECurrencyType, int> OnRewardGranted;
    public event Action<ECurrencyTransactionType, ECurrencyType, int, int> OnCurrencySpent;
    #endregion

    #region 인스펙터
    [Header("초기 잔고")]
    [SerializeField, Min(0)] private int _initialGold = 100;
    [SerializeField, Min(0)] private int _initialLegacyPoints;
    #endregion

    #region 내부 변수
    private readonly HashSet<string> _grantedRewardIds = new HashSet<string>();
    private int _gold;
    private int _legacyPoints;
    private bool _isSpending;
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
        InitializeBalances();
    }

    private void Start()
    {

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// [FIN-01-03] 지정한 재화 잔고 반환
    /// </summary>
    public int GetBalance(ECurrencyType type)
    {
        switch (type)
        {
            case ECurrencyType.Gold:
                return _gold;
            case ECurrencyType.LegacyPoint:
                return _legacyPoints;
            default:
                return 0;
        }
    }

    /// <summary>
    /// [FIN-01-02] 지정한 재화를 추가
    /// </summary>
    public bool TryAdd(ECurrencyType type, int amount)
    {
        if ((type != ECurrencyType.Gold && type != ECurrencyType.LegacyPoint) || amount <= 0)
        {
            return false;
        }

        long newAmount = (long)GetBalance(type) + amount;
        if (newAmount > int.MaxValue)
        {
            return false;
        }

        SetBalance(type, (int)newAmount);
        return true;
    }

    /// <summary>
    /// [FIN-03-01] 잔고 확인 후 지정한 재화 차감
    /// 음수 금액과 잔고 부족 요청 거부
    /// 성공한 지출은 잔고 변경 알림과 거래 알림 발생
    /// </summary>
    public bool TrySpend(ECurrencyType type, int amount)
    {
        return TrySpend(type, amount, ECurrencyTransactionType.Unspecified);
    }

    /// <summary>
    /// [FIN-03-01] 거래 유형과 잔고를 검증해 재화 지출
    /// 요청 금액 전체를 한 번에 차감해 부분 결제 방지
    /// 성공한 거래의 종류와 차감 후 잔고 전달
    /// </summary>
    public bool TrySpend(ECurrencyType type, int amount, ECurrencyTransactionType transactionType)
    {
        if ((type != ECurrencyType.Gold && type != ECurrencyType.LegacyPoint) ||
            !Enum.IsDefined(typeof(ECurrencyTransactionType), transactionType) ||
            amount <= 0 || _isSpending || GetBalance(type) < amount)
        {
            return false;
        }

        int remainingBalance = GetBalance(type) - amount;
        _isSpending = true;
        try
        {
            SetBalance(type, remainingBalance);
            OnCurrencySpent?.Invoke(transactionType, type, amount, remainingBalance);
        }
        finally
        {
            _isSpending = false;
        }

        return true;
    }

    /// <summary>
    /// [FIN-01-01][FIN-01-02] Inspector 초기 잔고로 재화 잔고 초기화
    /// </summary>
    public void ResetBalances()
    {
        SetBalance(ECurrencyType.Gold, Mathf.Max(0, _initialGold));
        SetBalance(ECurrencyType.LegacyPoint, Mathf.Max(0, _initialLegacyPoints));
    }

    /// <summary>
    /// [ROGUE-02-02] 다음 런을 위해 런 재화인 골드만 초기화
    /// 영구 재화인 레거시 포인트는 유지
    /// </summary>
    public void ResetRunGold()
    {
        SetBalance(ECurrencyType.Gold, Mathf.Max(0, _initialGold));
    }

    /// <summary>
    /// [FIN-01-02] 골드 잔고에 파산 패널티 적용 후 차감액 반환
    /// </summary>
    public int ApplyPenalty(float penaltyRate)
    {
        if (penaltyRate <= 0.0f)
        {
            return 0;
        }

        int penalty = Mathf.Clamp(Mathf.RoundToInt(_gold * penaltyRate), 0, _gold);
        if (penalty > 0)
        {
            SetBalance(ECurrencyType.Gold, _gold - penalty);
        }

        return penalty;
    }

    /// <summary>
    /// [FIN-02-02] 고유 보상 ID 기준 추가 재화 지급과 중복 방지
    /// </summary>
    public bool TryGrantReward(string rewardId, ECurrencyType type, int amount)
    {
        if (string.IsNullOrWhiteSpace(rewardId) || _grantedRewardIds.Contains(rewardId))
        {
            return false;
        }

        if (!TryAdd(type, amount))
        {
            return false;
        }

        _grantedRewardIds.Add(rewardId);
        OnRewardGranted?.Invoke(rewardId, type, amount);
        return true;
    }

    /// <summary>
    /// 저장된 골드와 레거시 포인트 복원
    /// 기존 잔액에 더하지 않고 저장된 값으로 설정
    /// </summary>
    /// <param name="gold"></param>
    /// <param name="legacyPoints"></param>
    /// <returns></returns>
    public bool RestoreBalances(int gold, int legacyPoints)
    {
        if (gold < 0 || legacyPoints < 0)
        {
            Debug.LogError("복원할 재화 값은 음수가 되면 안됌.");
            return false;
        }

        SetBalance(ECurrencyType.Gold, gold);
        SetBalance(ECurrencyType.LegacyPoint, legacyPoints);

        Debug.Log($"재화 복원 완료: 골드 {Gold}, 레거시 포인트 {LegacyPoints}");

        return true;
    }
    #endregion

    #region 커스텀 (Private)
    private void InitializeBalances()
    {
        _gold = Mathf.Max(0, _initialGold);
        _legacyPoints = Mathf.Max(0, _initialLegacyPoints);
    }

    /// <summary>
    /// 재화 잔고를 특정 금액으로 초기화
    /// </summary>
    /// <param name="type"></param>
    /// <param name="amount"></param>
    private void SetBalance(ECurrencyType type, int amount)
    {
        int previousAmount = GetBalance(type);
        if (previousAmount == amount)
        {
            return;
        }

        if (type == ECurrencyType.Gold)
        {
            _gold = amount;
            Debug.LogWarning($"현재 골드: {_gold}");
            OnAmountChanged?.Invoke(_gold);
        }
        else if (type == ECurrencyType.LegacyPoint)
        {
            _legacyPoints = amount;
        }
        else
        {
            return;
        }

        OnBalanceChanged?.Invoke(type, previousAmount, amount);
    }
    #endregion
}
