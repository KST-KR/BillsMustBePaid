using System;

/// <summary>
/// [BILL-01-02] 청구서 납부 상태
/// </summary>
public enum EBillStatus
{
    /// <summary>
    /// [BILL-01-02] 납부 대기
    /// </summary>
    Unpaid,
    /// <summary>
    /// [BILL-01-02] 납부 완료
    /// </summary>
    Paid
}

/// <summary>
/// [BILL-01-02] 발행된 청구서 한 건의 런타임 데이터
/// </summary>
[Serializable]
public class BillRecord
{
    public string BillId { get; private set; }
    public string BillTypeId { get; private set; }
    public string BillName { get; private set; }
    public int IssueDay { get; private set; }
    public int DueDay { get; private set; }
    public int DueDays { get; private set; }
    public int Amount { get; private set; }
    public int Cycle { get; private set; }
    public EBillPenaltyType PenaltyType { get; private set; }
    public float PenaltyRate { get; private set; }
    public int PerkRewardCount { get; private set; }
    public float LegacyPointRate { get; private set; }
    public float EarlyPaymentBonus { get; private set; }
    public EBillStatus Status { get; private set; }

    /// <summary>
    /// [BILL-01-02] 규칙 데이터에서 발행 시점의 청구서 기록 생성
    /// </summary>
    public BillRecord(BillData billData, int issueDay, int cycle, int amount)
    {
        BillId = Guid.NewGuid().ToString("N");
        BillTypeId = billData.BillId;
        BillName = billData.BillName;
        IssueDay = issueDay;
        DueDay = issueDay + billData.DueDays;
        DueDays = billData.DueDays;
        Amount = amount;
        Cycle = cycle;
        PenaltyType = billData.PenaltyType;
        PenaltyRate = billData.PenaltyRate;
        PerkRewardCount = billData.PerkRewardCount;
        LegacyPointRate = billData.LegacyPointRate;
        EarlyPaymentBonus = billData.EarlyPaymentBonus;
        Status = EBillStatus.Unpaid;
    }

    /// <summary>
    /// [BILL-01-02] 청구서 납부 상태 변경
    /// </summary>
    public void SetStatus(EBillStatus status)
    {
        Status = status;
    }
}
