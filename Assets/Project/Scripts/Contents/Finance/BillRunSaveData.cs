using System;
using System.Collections.Generic;

public enum ETonyLoanState
{
    Available,
    Outstanding,
    WaitingForRegularBill
}

[Serializable]
public sealed class BillRecordSaveData
{
    public string billId;
    public string billTypeId;
    public string category;
    public string billName;
    public int issueDay;
    public int dueDay;
    public int dueDays;
    public int amount;
    public int cycle;
    public EBillPenaltyType penaltyType;
    public float penaltyRate;
    public int perkRewardCount;
    public EBillStatus status;

    public BillRecordSaveData()
    {
    }

    public BillRecordSaveData(BillRecord bill)
    {
        billId = bill.BillId;
        billTypeId = bill.BillTypeId;
        category = bill.Category;
        billName = bill.BillName;
        issueDay = bill.IssueDay;
        dueDay = bill.DueDay;
        dueDays = bill.DueDays;
        amount = bill.Amount;
        cycle = bill.Cycle;
        penaltyType = bill.PenaltyType;
        penaltyRate = bill.PenaltyRate;
        perkRewardCount = bill.PerkRewardCount;
        status = bill.Status;
    }
}

[Serializable]
public sealed class BillRunSaveData
{
    public int currentDay;
    public ETonyLoanState tonyLoanState;
    public List<BillRecordSaveData> bills = new List<BillRecordSaveData>();
    public List<string> dueSoonNotifiedBillIds = new List<string>();
}
