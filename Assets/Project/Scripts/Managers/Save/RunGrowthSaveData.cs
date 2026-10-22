using System;
using System.Collections.Generic;

/// <summary>
/// [GROW-01-05] 런 성장 수치와 스킬 진행 정보를 저장
/// </summary>
[Serializable]
public class RunGrowthSaveData
{
    /// <summary>
    /// [GROW-01-05] 저장 시 선택되어 있던 스킬 ID
    /// </summary>
    public string SelectedUpgradeId;
    /// <summary>
    /// [GROW-01-05] 스킬별 레벨과 해금 기록
    /// </summary>
    public List<GrowthSkillSaveEntry> Skills = new List<GrowthSkillSaveEntry>();
}

/// <summary>
/// [GROW-01-05] 저장 대상 스킬 한 건의 진행 정보
/// </summary>
[Serializable]
public class GrowthSkillSaveEntry
{
    /// <summary>
    /// [GROW-01-05] 스킬 데이터 ID
    /// </summary>
    public string UpgradeId;
    /// <summary>
    /// [GROW-01-05] 저장된 스킬 레벨
    /// </summary>
    public int Level;
    /// <summary>
    /// [GROW-01-05] 저장된 해금 여부
    /// </summary>
    public bool IsUnlocked;

    /// <summary>
    /// [GROW-01-05] 직렬화 및 저장 데이터 복원용 기본 생성자
    /// </summary>
    public GrowthSkillSaveEntry()
    {
    }

    /// <summary>
    /// [GROW-01-05] ID, 레벨, 해금 여부로 저장 항목 생성
    /// </summary>
    public GrowthSkillSaveEntry(string upgradeId, int level, bool isUnlocked)
    {
        UpgradeId = upgradeId;
        Level = level;
        IsUnlocked = isUnlocked;
    }
}