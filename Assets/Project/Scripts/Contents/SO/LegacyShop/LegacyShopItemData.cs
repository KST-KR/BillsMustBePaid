using UnityEngine;

/// <summary>
/// 영구 레거시 상점 링과 구매 효과를 정의합니다.
/// </summary>
[CreateAssetMenu(fileName = "LEG_", menuName = "Legacy Shop/Legacy Shop Item")]
public sealed class LegacyShopItemData : ScriptableObject
{
    [SerializeField] private string _itemId;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField, Min(0)] private int _price;
    [SerializeField] private string _meshName;
    [SerializeField] private EUpgradeStat _targetStat;
    [SerializeField] private float _statValue;
    [SerializeField] private bool _initiallyOwned;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _lockedMaterial;

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public int Price => _price;
    public string FormattedPrice => _price.ToString("N0");
    public string MeshName => _meshName;
    public EUpgradeStat TargetStat => _targetStat;
    public float StatValue => _statValue;
    public bool InitiallyOwned => _initiallyOwned;
    public Material ActiveMaterial => _activeMaterial;
    public Material LockedMaterial => _lockedMaterial;

    public string StatEffectText
    {
        get
        {
            switch (_targetStat)
            {
                case EUpgradeStat.Attack:
                    return string.Format("공격력 +{0:0.##}", _statValue);
                case EUpgradeStat.AttackRange:
                    return string.Format("공격 범위 +{0:0.##}", _statValue);
                case EUpgradeStat.CriticalChance:
                    return string.Format("치명타 확률 +{0:0.#}%", _statValue * 100.0f);
                case EUpgradeStat.AttackSpeed:
                    return string.Format("공격 간격 {0:0.##}초 단축", Mathf.Abs(_statValue));
                case EUpgradeStat.Stamina:
                    return string.Format("최대 스태미나 +{0:0.##}", _statValue);
                case EUpgradeStat.Luck:
                    return string.Format("행운 +{0:0.##}", _statValue);
                default:
                    return string.Empty;
            }
        }
    }

    private void OnValidate()
    {
        _itemId = _itemId != null ? _itemId.Trim() : string.Empty;
        _displayName = _displayName != null ? _displayName.Trim() : string.Empty;
        _price = Mathf.Max(0, _price);
        _meshName = _meshName != null ? _meshName.Trim() : string.Empty;
        if (_description == null) _description = string.Empty;
    }
}
