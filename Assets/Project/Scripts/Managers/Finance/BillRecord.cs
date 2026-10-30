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
    Paid,
    /// <summary>
    /// [BILL-02-01] 납부 기한 만료
    /// </summary>
    Expired
}

/// <summary>
/// [BILL-01-02] 발행된 청구서 한 건의 런타임 데이터
/// </summary>
[Serializable]
public class BillRecord
{
    public string BillId { get; private set; }
    public string BillTypeId { get; private set; }
    public string Category { get; private set; }
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
        Category = billData.GetCategory(cycle);
        BillName = billData.GetTitle(cycle);
        IssueDay = issueDay;
        DueDay = issueDay + billData.DueDays;
        DueDays = billData.DueDays;
        Amount = amount;
        Cycle = cycle;
        PenaltyType = billData.PenaltyType;
        PenaltyRate = billData.GetPenaltyRate(cycle);
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

    /// <summary>
    /// [BILL-02-01] 현재 일자 기준 남은 납부 일수 반환
    /// </summary>
    public int GetRemainingDays(int currentDay)
    {
        return Math.Max(0, DueDay - currentDay);
    }

    /// <summary>
    /// [BILL-02-01] 현재 일자 기준 연체 일수 반환
    /// 납기일 당일은 연체로 계산하지 않음
    /// </summary>
    public int GetOverdueDays(int currentDay)
    {
        return Math.Max(0, currentDay - DueDay);
    }

    /// <summary>
    /// [BILL-02-01] 기본 패널티율에 연체 일수별 증가율을 더한 최종 수수료율 반환
    /// </summary>
    public float GetPenaltyRate(int currentDay)
    {
        if (PenaltyType != EBillPenaltyType.IncomeDeduction)
        {
            return 0f;
        }

        int overdueDays = GetOverdueDays(currentDay);
        int increasePercent = 0;
        for (int day = 0; day < overdueDays && increasePercent < 60; day++)
        {
            increasePercent = Math.Min(60, increasePercent * 2 + 1);
            // 증가율은 1, 3, 7, 15, 31, 최대 60% 순으로 적용
        }

        return (float)Math.Min(1.0, Math.Max(0.0, PenaltyRate) + increasePercent / 100.0);
    }
}
