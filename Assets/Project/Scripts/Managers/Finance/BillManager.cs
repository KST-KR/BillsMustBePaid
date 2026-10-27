using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BILL-01-02][BILL-01-03] 발행 조건에 따른 청구서 생성과 중복 방지 관리
/// </summary>
public class BillManager : MonoBehaviour
{
    #region 싱글톤
    public static BillManager Instance { get; private set; }
    #endregion

    #region 속성
    public IReadOnlyList<BillRecord> Bills => _bills;
    public BillRecord CurrentBill => GetNextUnpaidBill();
    public int CurrentDay => _currentDay;
    #endregion

    #region 이벤트
    public event Action<BillRecord> OnBillIssued;
    public event Action<BillRecord> OnBillPaid;
    public event Action<BillRecord> OnBillChanged;
    #endregion

    #region 인스펙터
    [SerializeField] private BillData[] _billRules = new BillData[0];

    [Header("청구서 납부 버튼")]
    [SerializeField] private Button _payBillButton;
    [SerializeField] private Image _payBillButtonImage;
    [SerializeField] private Sprite _unaffordableButtonSprite;
    [SerializeField] private Sprite _affordableButtonSprite;
    #endregion

    #region 내부 변수
    private readonly List<BillRecord> _bills = new List<BillRecord>();
    private readonly Dictionary<BillData, int> _issuedCounts = new Dictionary<BillData, int>();
    private readonly HashSet<string> _issuedKeys = new HashSet<string>();
    private int _currentDay;
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
        if (_billRules == null)
        {
            _billRules = new BillData[0];
        }
    }

    private void Start()
    {
        if (_payBillButton != null)
        {
            _payBillButton.onClick.AddListener(HandlePayButtonClicked);
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
        }

        RefreshPayButtonImage();
    }

    private void OnDestroy()
    {
        if (_payBillButton != null)
        {
            _payBillButton.onClick.RemoveListener(HandlePayButtonClicked);
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// [BILL-01-02][BILL-01-03] 게임 일차를 갱신하고 해당 일자 청구서 발행
    /// </summary>
    public void NotifyDayAdvanced(int gameDay)
    {
        if (gameDay <= _currentDay)
        {
            return;
        }

        for (int day = _currentDay + 1; day <= gameDay; day++)
        {
            _currentDay = day;
            IssueBillsForDay(day);
        }
    }

    /// <summary>
    /// [BILL-01-02] 현재 대기 청구서 한 건 납부 시도
    /// </summary>
    public bool TryPayCurrentBill()
    {
        BillRecord bill = GetNextUnpaidBill();
        if (bill == null || CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpend(ECurrencyType.Gold, bill.Amount))
        {
            return false;
        }

        bill.SetStatus(EBillStatus.Paid);
        OnBillPaid?.Invoke(bill);
        OnBillChanged?.Invoke(bill);
        RefreshPayButtonImage();
        return true;
    }

    /// <summary>
    /// [BILL-01-02] 런 청구서 발행 기록 초기화
    /// </summary>
    public void ResetRunBills()
    {
        _bills.Clear();
        _issuedCounts.Clear();
        _issuedKeys.Clear();
        _currentDay = 0;
        OnBillChanged?.Invoke(CurrentBill);
        RefreshPayButtonImage();
    }
    #endregion

    #region 커스텀 (Private)
    private bool IssueBill(BillData billData, int cycle)
    {
        if (billData == null || string.IsNullOrWhiteSpace(billData.BillId) || cycle < 1 ||
            GetIssuedCount(billData) >= Mathf.Max(1, billData.MaxIssues) ||
            !billData.TryGetAmount(cycle, out int amount))
        {
            return false;
        }

        string issueKey = billData.BillId + ":" + cycle;
        if (_issuedKeys.Contains(issueKey))
        {
            return false;
        }

        BillRecord bill = new BillRecord(billData, _currentDay, cycle, amount);
        _bills.Add(bill);
        _issuedKeys.Add(issueKey);
        SetIssuedCount(billData, GetIssuedCount(billData) + 1);
        OnBillIssued?.Invoke(bill);
        OnBillChanged?.Invoke(bill);
        RefreshPayButtonImage();
        return true;
    }

    private void HandlePayButtonClicked()
    {
        TryPayCurrentBill();
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        if (type == ECurrencyType.Gold)
        {
            RefreshPayButtonImage();
        }
    }

    private void RefreshPayButtonImage()
    {
        if (_payBillButtonImage == null)
        {
            return;
        }

        BillRecord bill = CurrentBill;
        bool canPay = bill != null && CurrencyManager.Instance != null && CurrencyManager.Instance.Gold >= bill.Amount;
        Sprite stateSprite = canPay ? _affordableButtonSprite : _unaffordableButtonSprite;
        if (stateSprite != null)
        {
            _payBillButtonImage.sprite = stateSprite;
        }
    }

    private void IssueBillsForDay(int gameDay)
    {
        for (int i = 0; i < _billRules.Length; i++)
        {
            BillData billData = _billRules[i];
            if (billData != null && billData.IssueDay == gameDay && GetIssuedCount(billData) == 0)
            {
                IssueBill(billData, GetIssuedCount(billData) + 1);
            }
        }
    }

    private int GetIssuedCount(BillData billData)
    {
        return _issuedCounts.TryGetValue(billData, out int count) ? count : 0;
    }

    private void SetIssuedCount(BillData billData, int count)
    {
        _issuedCounts[billData] = count;
    }

    private BillRecord GetNextUnpaidBill()
    {
        for (int i = 0; i < _bills.Count; i++)
        {
            if (_bills[i].Status == EBillStatus.Unpaid)
            {
                return _bills[i];
            }
        }

        return null;
    }
    #endregion
}
