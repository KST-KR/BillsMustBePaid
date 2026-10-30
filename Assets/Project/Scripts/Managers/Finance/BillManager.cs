using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BILL-01-02][BILL-01-03][BILL-01-04][BILL-01-05][BILL-02-01] 청구서 발행과 납부 기한 관리
/// 런 진행 일자에 맞춰 청구 대기열과 UI 이벤트 갱신
/// </summary>
public class BillManager : MonoBehaviour
{
    #region 싱글톤
    public static BillManager Instance { get; private set; }
    #endregion

    #region 속성
    public IReadOnlyList<BillRecord> Bills => _bills;
    public IReadOnlyList<BillRecord> UnpaidBills => GetUnpaidBills();
    public BillRecord CurrentBill => GetNextUnpaidBill();
    public int CurrentDay => _currentDay;
    public bool HasExpiredBills
    {
        get
        {
            for (int i = 0; i < _bills.Count; i++)
            {
                if (_bills[i].Status == EBillStatus.Expired)
                {
                    return true;
                }
            }

            return false;
        }
    }
    #endregion

    #region 이벤트
    public event Action<BillRecord> OnBillIssued;
    public event Action<BillRecord> OnBillPaid;
    public event Action<BillRecord> OnBillChanged;
    public event Action<BillRecord> OnBillDueSoon;
    public event Action<BillRecord> OnBillExpired;
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
    private readonly HashSet<string> _dueSoonNotifiedIds = new HashSet<string>();
    private StageManager _stageManager;
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

        _stageManager = StageManager.Instance;
        if (_stageManager != null)
        {
            _stageManager.OnStageStarted += HandleStageStarted;
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

        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
        }


        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// [BILL-02-01] 게임 일차 갱신과 청구 발행 및 기한 판정
    /// 만기 임박과 기한 만료를 한 번만 이벤트로 전달
    /// 기한 내 납부 기회를 위해 납기일 다음 날 만료 처리
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
            CheckBillDeadlines(day);
        }

        OnBillChanged?.Invoke(CurrentBill);
        RefreshPayButtonImage();
    }

    /// <summary>
    /// [BILL-01-04] 만기순 대기열의 최우선 청구서 한 건 납부 시도
    /// 결제 성공 시 상태 변경과 다음 회차 발행 이벤트 전달
    /// 잔고 부족 또는 활성 런 외의 요청은 거부
    /// </summary>
    public bool TryPayCurrentBill()
    {
        BillRecord bill = GetNextUnpaidBill();
        if (bill == null || CurrencyManager.Instance == null ||
            (GameManager.Instance != null && !GameManager.Instance.IsRunActive) ||
            !CurrencyManager.Instance.TrySpend(ECurrencyType.Gold, bill.Amount, ECurrencyTransactionType.BillPayment))
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
    /// [BILL-01-04] 청구서 패널의 납부 요청 처리
    /// 대기열에서 가장 이른 납기 청구서만 납부
    /// </summary>
    public bool TryPayBill(string billId)
    {
        BillRecord bill = CurrentBill;
        if (bill == null || bill.BillId != billId)
        {
            return false;
        }

        return TryPayCurrentBill();
    }

    /// <summary>
    /// [BILL-01-04] 미납 청구서를 납기일 오름차순으로 조회
    /// 납기일이 같으면 발행일 순으로 정렬
    /// </summary>
    public IReadOnlyList<BillRecord> GetUnpaidBills()
    {
        List<BillRecord> unpaidBills = new List<BillRecord>();
        for (int i = 0; i < _bills.Count; i++)
        {
            if (_bills[i].Status == EBillStatus.Unpaid)
            {
                unpaidBills.Add(_bills[i]);
            }
        }

        unpaidBills.Sort((left, right) =>
        {
            int dueDayComparison = left.DueDay.CompareTo(right.DueDay);
            return dueDayComparison != 0 ? dueDayComparison : left.IssueDay.CompareTo(right.IssueDay);
        });
        return unpaidBills;
    }

    /// <summary>
    /// [BILL-02-01] 스테이지 총수입 기준 미납 만료 청구서의 연체료 계산
    /// 각 청구서의 기본 패널티율에 1, 3, 7, 15, 31, 최대 60%의 연체 증가율을 더함
    /// </summary>
    public int CalculateOverdueIncomePenalty(int income)
    {
        if (income <= 0)
        {
            return 0;
        }

        long totalPenalty = 0;
        for (int i = 0; i < _bills.Count; i++)
        {
            BillRecord bill = _bills[i];
            if (bill.Status != EBillStatus.Expired || bill.PenaltyType != EBillPenaltyType.IncomeDeduction)
            {
                continue;
            }

            float rate = bill.GetPenaltyRate(_currentDay);
            if (rate >= 1f)
            {
                return income;
            }

            totalPenalty += Mathf.RoundToInt(income * rate);
            if (totalPenalty >= income)
            {
                return income;
            }
        }

        return (int)totalPenalty;
    }

    /// <summary>
    /// [BILL-01-05] 납부한 청구서의 다음 회차까지 남은 일수 반환
    /// 다음 회차가 없거나 예정일을 알 수 없으면 -1 반환
    /// </summary>
    public int GetDaysUntilNextBill(BillRecord paidBill)
    {
        if (paidBill == null || paidBill.Status != EBillStatus.Paid)
        {
            return -1;
        }

        for (int i = 0; i < _billRules.Length; i++)
        {
            BillData billData = _billRules[i];
            if (billData == null || billData.BillId != paidBill.BillTypeId ||
                paidBill.Cycle >= billData.MaxIssues)
            {
                continue;
            }

            int nextIssueDay = paidBill.DueDay + 1;
            return Math.Max(0, nextIssueDay - _currentDay);
        }

        return -1;
    }

    /// <summary>
    /// [BILL-01-04] 발행된 청구서를 고유 ID로 조회
    /// 납부 및 만료 상태를 포함한 현재 기록 반환
    /// </summary>
    public BillRecord GetBillById(string billId)
    {
        if (string.IsNullOrWhiteSpace(billId))
        {
            return null;
        }

        for (int i = 0; i < _bills.Count; i++)
        {
            if (_bills[i].BillId == billId)
            {
                return _bills[i];
            }
        }

        return null;
    }

    /// <summary>
    /// [BILL-01-04] 런 시작 시 청구 대기열과 중복 기록 초기화
    /// 다음 런의 일차 및 기한 알림 기록 재설정
    /// 현재 화면과 납부 버튼 상태 갱신
    /// </summary>
    public void ResetRunBills()
    {
        _bills.Clear();
        _issuedCounts.Clear();
        _issuedKeys.Clear();
        _dueSoonNotifiedIds.Clear();
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

    private void HandleStageStarted(int stageNumber)
    {
        if (stageNumber == 1)
        {
            ResetRunBills();
        }

        NotifyDayAdvanced(stageNumber);
    }

    private void CheckBillDeadlines(int gameDay)
    {
        for (int i = 0; i < _bills.Count; i++)
        {
            BillRecord bill = _bills[i];
            if (bill.Status != EBillStatus.Unpaid)
            {
                continue;
            }

            int remainingDays = bill.GetRemainingDays(gameDay);
            if (remainingDays <= 1 && _dueSoonNotifiedIds.Add(bill.BillId))
            {
                OnBillDueSoon?.Invoke(bill);
            }

            if (gameDay <= bill.DueDay)
            {
                continue;
            }

            bill.SetStatus(EBillStatus.Expired);
            Debug.LogWarning($"청구서 기한 만료: {bill.BillName}");
            OnBillExpired?.Invoke(bill);
            OnBillChanged?.Invoke(bill);
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
            if (billData == null)
            {
                continue;
            }

            int issuedCount = GetIssuedCount(billData);
            int nextCycle = issuedCount + 1;
            if (nextCycle > billData.MaxIssues)
            {
                continue;
            }

            int scheduledDay = billData.IssueDay;
            if (issuedCount > 0)
            {
                BillRecord previousCycle = FindBillRecord(billData.BillId, issuedCount);
                if (previousCycle == null || previousCycle.Status != EBillStatus.Paid)
                {
                    continue;
                }

                scheduledDay = previousCycle.DueDay + 1;
            }

            if (scheduledDay == gameDay)
            {
                IssueBill(billData, nextCycle);
            }
        }
    }

    private BillRecord FindBillRecord(string billTypeId, int cycle)
    {
        for (int i = _bills.Count - 1; i >= 0; i--)
        {
            BillRecord bill = _bills[i];
            if (bill.BillTypeId == billTypeId && bill.Cycle == cycle)
            {
                return bill;
            }
        }

        return null;
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
        BillRecord nextBill = null;
        for (int i = 0; i < _bills.Count; i++)
        {
            BillRecord candidate = _bills[i];
            if (candidate.Status != EBillStatus.Unpaid)
            {
                continue;
            }

            if (nextBill == null || candidate.DueDay < nextBill.DueDay ||
                (candidate.DueDay == nextBill.DueDay && candidate.IssueDay < nextBill.IssueDay))
            {
                nextBill = candidate;
            }
        }

        return nextBill;
    }
    #endregion
}
