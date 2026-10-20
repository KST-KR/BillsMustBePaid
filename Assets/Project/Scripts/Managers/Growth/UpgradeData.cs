using UnityEngine;

/// <summary>
/// [GROW-01-01] 스킬 트리 노드 및 레벨별 성장 데이터 보유
/// </summary>
[CreateAssetMenu(fileName = "UpgradeData", menuName = "Bills Must Be Paid/Upgrade")]
public class UpgradeData : ScriptableObject
{
    [System.Serializable]
    public struct PrerequisiteSkill
    {
        public string upgradeId;
        public int requiredLevel;
    }

    #region 속성
    public string UpgradeId => _upgradeId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public EUpgradeStat TargetStat => _targetStat;
    public int Cost => _cost;
    public float ValuePerLevel => _valuePerLevel;
    public int MaxLevel => _maxLevel;
    public PrerequisiteSkill[] PrerequisiteIds => _prerequisiteIds;
    public Sprite Icon => _icon;
    public Vector2 TreePosition => _treePosition;
    #endregion

    #region 인스펙터
    [SerializeField] private string _upgradeId;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private EUpgradeStat _targetStat;
    [SerializeField, Min(0)] private int _cost = 100;
    [SerializeField] private float _valuePerLevel = 1.0f;
    [SerializeField, Min(1)] private int _maxLevel = 10;
    [SerializeField] private PrerequisiteSkill[] _prerequisiteIds = new PrerequisiteSkill[0];
    [SerializeField] private Sprite _icon;
    [SerializeField] private Vector2 _treePosition;
    #endregion
}

