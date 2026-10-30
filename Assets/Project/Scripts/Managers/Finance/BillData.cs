using UnityEngine;

/// <summary>
/// [BILL-01-01] 한 회차 청구서의 금액과 표시 데이터를 보유
/// </summary>
[System.Serializable]
public class BillIssueData
{
    [SerializeField, Min(0)] private int _amount;
    [SerializeField] private string _category;
    [SerializeField] private string _title;
    [SerializeField, Range(0.0f, 1.0f)] private float _penaltyRate;

    public int Amount => _amount;
    public string Category => _category;
    public string Title => _title;
    public float PenaltyRate => _penaltyRate;

    public BillIssueData()
    {
    }

    public BillIssueData(int amount, string category, string title)
        : this(amount, category, title, 0.0f)
    {
    }

    public BillIssueData(int amount, string category, string title, float penaltyRate)
    {
        _amount = amount;
        _category = category;
        _title = title;
        _penaltyRate = penaltyRate;
    }
}

/// <summary>
/// [BILL-01-01] 청구서의 식별/발행/납부/보상 데이터 보유
/// </summary>
[CreateAssetMenu(fileName = "BillData", menuName = "Bills Must Be Paid/Bill")]
public class BillData : ScriptableObject
{
    #region 속성
    public string BillId => _billId;
    public string BillName => _billName;
    public int IssueDay => _issueDay;
    public int DueDays => _dueDays;
    public int[] BillAmounts => GetBillAmounts();
    public int IssueCount => _issues != null && _issues.Length > 0
        ? _issues.Length
        : _billAmounts != null ? _billAmounts.Length : 0;
    public EBillPenaltyType PenaltyType => _penaltyType;
    public int PerkRewardCount => _perkRewardCount;
    public float LegacyPointRate => _legacyPointRate;
    public float EarlyPaymentBonus => _earlyPaymentBonus;
    public int MaxIssues => _maxIssues;
    #endregion

    #region 인스펙터
    [SerializeField] private string _billId;
    [SerializeField] private string _billName;
    [SerializeField, Min(1)] private int _issueDay = 1;
    [SerializeField, Min(0)] private int _dueDays;
    [SerializeField] private BillIssueData[] _issues = new BillIssueData[0];
    [SerializeField] private EBillPenaltyType _penaltyType;
    // 구버전 에셋 호환용 기본 패널티율. 새 데이터는 회차별 BillIssueData에 저장.
    [SerializeField, HideInInspector, Min(0.0f)] private float _penaltyRate;
    [SerializeField, Min(0)] private int _perkRewardCount;
    [SerializeField, Min(0.0f)] private float _legacyPointRate;
    [SerializeField, Min(0.0f)] private float _earlyPaymentBonus;
    [SerializeField, Min(1)] private int _maxIssues = 1;
    // 구버전 에셋을 새 모델로 옮기기 위한 숨김 필드. 재저장 이후에는 비워짐.
    [SerializeField, HideInInspector] private int[] _billAmounts = new int[0];
    [SerializeField, HideInInspector] private string[] _billCategories = new string[0];
    [SerializeField, HideInInspector] private string[] _billTitles = new string[0];
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// [BILL-01-01] 지정 회차에 설정된 청구 금액 반환
    /// </summary>
    public bool TryGetAmount(int cycle, out int amount)
    {
        amount = 0;
        if (cycle < 1 || cycle > IssueCount)
        {
            return false;
        }

        BillIssueData issue = GetIssue(cycle);
        if (issue != null)
        {
            amount = issue.Amount;
        }
        else if (_billAmounts != null && cycle <= _billAmounts.Length)
        {
            amount = _billAmounts[cycle - 1];
        }
        else
        {
            return false;
        }

        return amount > 0;
    }

    /// <summary>
    /// [BILL-01-01] 회차별 청구 카테고리를 반환
    /// </summary>
    public string GetCategory(int cycle)
    {
        BillIssueData issue = GetIssue(cycle);
        if (issue != null && !string.IsNullOrWhiteSpace(issue.Category))
        {
            return issue.Category;
        }

        if (issue == null && _billCategories != null && cycle > 0 && cycle <= _billCategories.Length &&
            !string.IsNullOrWhiteSpace(_billCategories[cycle - 1]))
        {
            return _billCategories[cycle - 1];
        }

        return $"{_billId}-{cycle:00}";
    }

    /// <summary>
    /// [BILL-01-01] 회차별 청구서 제목을 반환
    /// </summary>
    public string GetTitle(int cycle)
    {
        BillIssueData issue = GetIssue(cycle);
        if (issue != null && !string.IsNullOrWhiteSpace(issue.Title))
        {
            return issue.Title;
        }

        if (issue == null && _billTitles != null && cycle > 0 && cycle <= _billTitles.Length &&
            !string.IsNullOrWhiteSpace(_billTitles[cycle - 1]))
        {
            return _billTitles[cycle - 1];
        }

        return $"{_billName} {cycle}회차";
    }

    /// <summary>
    /// [BILL-01-01] 회차별 데이터를 반환
    /// </summary>
    public BillIssueData GetIssue(int cycle)
    {
        if (_issues == null || cycle < 1 || cycle > _issues.Length)
        {
            return null;
        }

        return _issues[cycle - 1];
    }

    /// <summary>
    /// [BILL-01-01] 회차별 기본 연체 패널티율 반환
    /// </summary>
    public float GetPenaltyRate(int cycle)
    {
        BillIssueData issue = GetIssue(cycle);
        if (issue != null && issue.PenaltyRate > 0.0f)
        {
            return issue.PenaltyRate;
        }

        return Mathf.Max(0.0f, _penaltyRate);
    }

    private int[] GetBillAmounts()
    {
        if (_issues == null || _issues.Length == 0)
        {
            return _billAmounts ?? new int[0];
        }

        int[] amounts = new int[_issues.Length];
        for (int i = 0; i < _issues.Length; i++)
        {
            amounts[i] = _issues[i] != null ? _issues[i].Amount : 0;
        }

        return amounts;
    }
    #endregion
}
