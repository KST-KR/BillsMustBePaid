using UnityEngine;

/// <summary>
/// [BILL-01-01] 연체 시 적용할 불이익 종류
/// </summary>
public enum EBillPenaltyType
{
    /// <summary>
    /// [BILL-01-01] 별도 불이익 없음
    /// </summary>
    None,
    /// <summary>
    /// [BILL-01-01] 수입 일부 차감 방식
    /// </summary>
    IncomeDeduction
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
    public int[] BillAmounts => _billAmounts;
    public EBillPenaltyType PenaltyType => _penaltyType;
    public float PenaltyRate => _penaltyRate;
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
    [SerializeField, Min(0)] private int[] _billAmounts = new int[0];
    [SerializeField] private EBillPenaltyType _penaltyType;
    [SerializeField, Min(0.0f)] private float _penaltyRate;
    [SerializeField, Min(0)] private int _perkRewardCount;
    [SerializeField, Min(0.0f)] private float _legacyPointRate;
    [SerializeField, Min(0.0f)] private float _earlyPaymentBonus;
    [SerializeField, Min(1)] private int _maxIssues = 1;
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// [BILL-01-01] 지정 회차에 설정된 청구 금액 반환
    /// </summary>
    public bool TryGetAmount(int cycle, out int amount)
    {
        amount = 0;
        if (_billAmounts == null || cycle < 1 || cycle > _billAmounts.Length)
        {
            return false;
        }

        amount = _billAmounts[cycle - 1];
        return amount > 0;
    }
    #endregion
}
