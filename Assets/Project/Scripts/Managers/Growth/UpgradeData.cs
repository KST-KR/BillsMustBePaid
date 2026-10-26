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
/// [GROW-01-01] 스킬 노드 및 성장 데이터를 보유하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_", menuName = "Growth/Upgrade Data")]
public sealed class UpgradeData : ScriptableObject
{
    [SerializeField] private string _upgradeId;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private EUpgradeStat _targetStat;
    [SerializeField, Min(0)] private int _cost;
    [SerializeField] private float _valuePerLevel;
    [SerializeField, Min(1)] private int _maxLevel = 1;
    [SerializeField] private PrerequisiteUpgrade[] _prerequisiteIds = new PrerequisiteUpgrade[0];
    [SerializeField] private Sprite _icon;
    [SerializeField] private Vector2 _treePosition;
    [SerializeField] private ESkillEffectMode _effectMode;

    #region 속성
    /// <summary>
    /// [GROW-01-01] 고유 스킬 식별자
    /// </summary>
    public string UpgradeId => _upgradeId;
    /// <summary>
    /// [GROW-01-01] 화면 표시 이름
    /// </summary>
    public string DisplayName => _displayName;
    /// <summary>
    /// [GROW-01-01] 스킬 설명
    /// </summary>
    public string Description => _description;
    /// <summary>
    /// [GROW-01-01] 성장 대상 능력치
    /// </summary>
    public EUpgradeStat TargetStat => _targetStat;
    /// <summary>
    /// [GROW-01-01] 레벨 구매 비용
    /// </summary>
    public int Cost => _cost;
    /// <summary>
    /// [GROW-01-01] 레벨당 효과 수치
    /// </summary>
    public float ValuePerLevel => _valuePerLevel;
    /// <summary>
    /// [GROW-01-01] 최대 레벨
    /// </summary>
    public int MaxLevel => _maxLevel;
    /// <summary>
    /// [GROW-01-01] 선행 스킬 조건
    /// </summary>
    public PrerequisiteUpgrade[] PrerequisiteIds => _prerequisiteIds;
    /// <summary>
    /// [GROW-02-01] 스킬 아이콘
    /// </summary>
    public Sprite Icon => _icon;
    /// <summary>
    /// [GROW-02-01] 성장 트리 화면 위치
    /// </summary>
    public Vector2 TreePosition => _treePosition;
    /// <summary>
    /// [GROW-01-01] 스킬 효과 적용 방식
    /// </summary>
    public ESkillEffectMode EffectMode => _effectMode;
    #endregion

    private void OnValidate()
    {
        _upgradeId = _upgradeId != null ? _upgradeId.Trim() : string.Empty;
        _cost = Mathf.Max(0, _cost);
        _maxLevel = Mathf.Max(1, _maxLevel);
        if (_prerequisiteIds == null)
        {
            _prerequisiteIds = new PrerequisiteUpgrade[0];
        }
    }
}

