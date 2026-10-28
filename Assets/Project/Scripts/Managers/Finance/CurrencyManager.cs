using System;
using UnityEngine;

/// <summary>
/// [FIN-01-01][FIN-01-02][FIN-01-03] 재화 잔고 조회와 변경 이벤트 관리
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
    #endregion

    #region 인스펙터
    [Header("초기 잔고")]
    [SerializeField, Min(0)] private int _initialGold = 100;
    [SerializeField, Min(0)] private int _initialLegacyPoints;
    #endregion

    #region 내부 변수
    private int _gold;
    private int _legacyPoints;
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
    /// [FIN-01-02] 잔고를 확인하고 지정한 재화 차감
    /// </summary>
    public bool TrySpend(ECurrencyType type, int amount)
    {
        if (amount <= 0 || GetBalance(type) < amount)
        {
            return false;
        }

        SetBalance(type, GetBalance(type) - amount);
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
