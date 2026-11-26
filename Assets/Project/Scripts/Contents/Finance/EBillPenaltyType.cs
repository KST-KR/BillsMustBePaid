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