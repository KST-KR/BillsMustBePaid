using UnityEngine;

/// <summary>
/// [GROW-01-01] 요구 사항 스킬 구조체
/// </summary>
[System.Serializable]
public struct PrerequisiteUpgrade
{
    /// <summary>
    /// [GROW-01-01] 선행 스킬 식별자
    /// </summary>
    public string upgradeId;
    /// <summary>
    /// [GROW-01-01] 선행 스킬 필요 레벨
    /// </summary>
    public int requiredLevel;
}

/// <summary>
/// [GROW-01-01] 스킬 효과 적용 방식 구분
/// </summary>
public enum ESkillEffectMode
{
    /// <summary>
    /// [PL-SKILL-02] 해금 시 플레이어 능력치에 반영하는 방식
    /// </summary>
    PlayerStat,
    /// <summary>
    /// [PL-SKILL-02] TODO 피격 시점 계산 방식은 전투 담당과 계약 확정 후 구현
    /// </summary>
    HitCalculation
}

/// <summary>
/// [GROW-01-01] 코드 목업에서 생성하는 스킬 노드 및 성장 데이터 보유
/// </summary>
public sealed class UpgradeData
{
    #region 속성
    /// <summary>
    /// [GROW-01-01] 고유 스킬 식별자
    /// </summary>
    public string UpgradeId { get; private set; }
    /// <summary>
    /// [GROW-01-01] 화면 표시 이름
    /// </summary>
    public string DisplayName { get; private set; }
    /// <summary>
    /// [GROW-01-01] 스킬 설명
    /// </summary>
    public string Description { get; private set; }
    /// <summary>
    /// [GROW-01-01] 성장 대상 능력치
    /// </summary>
    public EUpgradeStat TargetStat { get; private set; }
    /// <summary>
    /// [GROW-01-01] 레벨 구매 비용
    /// </summary>
    public int Cost { get; private set; }
    /// <summary>
    /// [GROW-01-01] 레벨당 효과 수치
    /// </summary>
    public float ValuePerLevel { get; private set; }
    /// <summary>
    /// [GROW-01-01] 최대 레벨
    /// </summary>
    public int MaxLevel { get; private set; }
    /// <summary>
    /// [GROW-01-01] 선행 스킬 조건
    /// </summary>
    public PrerequisiteUpgrade[] PrerequisiteIds { get; private set; }
    /// <summary>
    /// [GROW-02-01] 스킬 아이콘
    /// </summary>
    public Sprite Icon { get; private set; }
    /// <summary>
    /// [GROW-02-01] 성장 트리 화면 위치
    /// </summary>
    public Vector2 TreePosition { get; private set; }
    /// <summary>
    /// [GROW-01-01] 스킬 효과 적용 방식
    /// </summary>
    public ESkillEffectMode EffectMode { get; private set; }
    #endregion

    #region 생성자
    /// <summary>
    /// [GROW-01-01] 런타임 목업용 스킬 데이터 생성
    /// </summary>
    public UpgradeData(string upgradeId, string displayName, string description, EUpgradeStat targetStat, int cost, float valuePerLevel, 
        int maxLevel, ESkillEffectMode effectMode, Vector2 treePosition, PrerequisiteUpgrade[] prerequisiteIds = null)
    {
        UpgradeId = upgradeId;
        DisplayName = displayName;
        Description = description;
        TargetStat = targetStat;
        Cost = Mathf.Max(0, cost);
        ValuePerLevel = valuePerLevel;
        MaxLevel = Mathf.Max(1, maxLevel);
        EffectMode = effectMode;
        TreePosition = treePosition;
        PrerequisiteIds = prerequisiteIds ?? new PrerequisiteUpgrade[0];
    }
    #endregion
}

