using UnityEngine;

public enum ERunEndReason
{
    Unknown,
    StageCleared,
    Bankrupt,
    GiveUp
}

/// <summary>
/// [ROGUE-01-01] 런 종료 사유와 획득 골드 기준 정산 규칙
/// </summary>
[CreateAssetMenu(fileName = "RunSettlementRules", menuName = "Bills Must Be Paid/Run Settlement Rules")]
public class RunSettlementRules : ScriptableObject
{
    [Header("레거시 포인트 환산")]
    [SerializeField, Min(1)] private int _goldPerLegacyPoint = 50;

    [Header("런 종료 사유")]
    [SerializeField] private bool _settleStageClearedRuns;
    [SerializeField] private bool _settleBankruptRuns;
    [SerializeField] private bool _settleGiveUpRuns;

    /// <summary>
    /// [ROGUE-01-01] 종료 사유와 획득 골드로 레거시 포인트 정산량 계산
    /// </summary>
    public int CalculateLegacyPoints(ERunEndReason endReason, int goldEarned)
    {
        if (goldEarned <= 0 || _goldPerLegacyPoint <= 0 || !IsEligibleEndReason(endReason))
        {
            return 0;
        }

        return goldEarned / _goldPerLegacyPoint;
    }

    private bool IsEligibleEndReason(ERunEndReason endReason)
    {
        switch (endReason)
        {
            case ERunEndReason.StageCleared:
                return _settleStageClearedRuns;
            case ERunEndReason.Bankrupt:
                return _settleBankruptRuns;
            case ERunEndReason.GiveUp:
                return _settleGiveUpRuns;
            default:
                return false;
        }
    }
}
