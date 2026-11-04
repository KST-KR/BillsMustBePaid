using UnityEngine;

/// <summary>
/// [GROW-03-01] 스킬 세트가 요구하는 개별 스킬과 최소 레벨을 정의
/// </summary>
[System.Serializable]
public struct SkillSetRequirement
{
    [SerializeField] private UpgradeData _upgrade;
    [SerializeField, Min(1)] private int _requiredLevel;

    public SkillSetRequirement(UpgradeData upgrade, int requiredLevel)
    {
        _upgrade = upgrade;
        _requiredLevel = Mathf.Max(1, requiredLevel);
    }

    public string UpgradeId => _upgrade != null ? _upgrade.UpgradeId : string.Empty;

    public int RequiredLevel => Mathf.Max(1, _requiredLevel);
}

/// <summary>
/// [GROW-03-01] 스킬 세트가 제공하는 능력치와 보너스 수치를 정의
/// </summary>
[System.Serializable]
public struct SkillSetBonus
{
    [SerializeField] private EUpgradeStat _targetStat;
    [SerializeField] private float _bonusValue;

    public SkillSetBonus(EUpgradeStat targetStat, float bonusValue)
    {
        _targetStat = targetStat;
        _bonusValue = bonusValue;
    }

    public EUpgradeStat TargetStat => _targetStat;

    public float BonusValue => _bonusValue;
}

/// <summary>
/// [GROW-03-01] 스킬세트 요구 조건과 추가 능력치 효과를 보유
/// </summary>
[CreateAssetMenu(fileName = "GrowthSkillSet_", menuName = "Growth/Skill Set Data")]
public sealed class GrowthSkillSetData : ScriptableObject
{
    [SerializeField] private string _setId;
    [SerializeField] private string _displayName;
    [SerializeField] private SkillSetRequirement[] _requirements = new SkillSetRequirement[0];
    [SerializeField] private SkillSetBonus[] _bonuses = new SkillSetBonus[0];

    public string SetId => _setId;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? _setId : _displayName;

    public SkillSetRequirement[] Requirements => _requirements;

    public SkillSetBonus[] Bonuses => _bonuses;

    private void OnValidate()
    {
        _setId = _setId != null ? _setId.Trim() : string.Empty;
        _displayName = _displayName != null ? _displayName.Trim() : string.Empty;
        if (_requirements == null)
        {
            _requirements = new SkillSetRequirement[0];
        }
        if (_bonuses == null)
        {
            _bonuses = new SkillSetBonus[0];
        }
    }
}