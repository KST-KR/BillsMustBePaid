using UnityEngine;

/// <summary>
/// 고지서 납부 보상 퍼크 데이터
/// </summary>
[CreateAssetMenu(fileName = "PERK_", menuName = "Bills Must Be Paid/Perk")]
public sealed class PerkData : ScriptableObject
{
    [SerializeField] private string _perkId;
    [SerializeField] private string _displayName;
    [SerializeField] private EPerkTargetStat _targetStat;
    [SerializeField] private EPerkValueMode _valueMode;
    [SerializeField] private float _value;
    [SerializeField, Range(1, 5)] private int _tier = 1;
    [SerializeField] private Sprite _icon;

    public string PerkId => _perkId;
    public string DisplayName => _displayName;
    public EPerkTargetStat TargetStat => _targetStat;
    public EPerkValueMode ValueMode => _valueMode;
    public float Value => _value;
    public int Tier => _tier;
    public Sprite Icon => _icon;

    public string ValueText
    {
        get
        {
            if (_valueMode == EPerkValueMode.Percent)
            {
                return $"+{_value * 100.0f:0.#}%";
            }

            return _targetStat == EPerkTargetStat.AttackSpeed ? $"-{Mathf.Abs(_value):0.###}" : $"+{_value:0.###}";
        }
    }

    public string StatText
    {
        get
        {
            switch (_targetStat)
            {
                case EPerkTargetStat.Attack:
                    return "공격력";
                case EPerkTargetStat.AttackSpeed:
                    return "공격속도";
                case EPerkTargetStat.Stamina:
                    return "스태미나";
                case EPerkTargetStat.AttackRange:
                    return "공격범위";
                case EPerkTargetStat.Reward:
                    return "보상증가";
                case EPerkTargetStat.CriticalChance:
                    return "치명타확률";
                default:
                    return string.Empty;
            }
        }
    }

    public string EffectText => $"{StatText} {ValueText}";

    private void OnValidate()
    {
        _perkId = _perkId != null ? _perkId.Trim() : string.Empty;
        _displayName = _displayName != null ? _displayName.Trim() : string.Empty;
        _tier = Mathf.Clamp(_tier, 1, 5);
    }
}
