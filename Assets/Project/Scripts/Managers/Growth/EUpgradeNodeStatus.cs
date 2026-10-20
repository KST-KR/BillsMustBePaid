/// <summary>
/// [GROW-01-01] 스킬 트리 노드의 현재 상태
/// </summary>
public enum EUpgradeNodeStatus
{
    /// <summary>
    /// [GROW-01-01] 선행 조건 미충족으로 잠김
    /// </summary>
    Locked,
    /// <summary>
    /// [GROW-01-01] 해금 조건 충족
    /// </summary>
    Unlocked,
    /// <summary>
    /// [GROW-01-01] 한 번 이상 학습 완료
    /// </summary>
    Learned,
    /// <summary>
    /// [GROW-01-01] 재화가 충분해 강화 가능
    /// </summary>
    Enhanceable,
    /// <summary>
    /// [GROW-01-01] 최대 레벨 도달
    /// </summary>
    MaxLevel
}