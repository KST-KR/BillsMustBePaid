using UnityEngine;

/// <summary>
/// 업그레이드 가능한 능력치 종류
/// </summary>
public enum EUpgradeStat
{
    /// <summary>
    /// 공격력
    /// </summary>
    Attack,
    /// <summary>
    /// 스태미나
    /// </summary>
    Stamina,
    /// <summary>
    /// 공격속도
    /// </summary>
    AttackSpeed,
    /// <summary>
    /// 공격범위
    /// </summary>
    AttackRange,
    /// <summary>
    /// 크리티컬 확률
    /// </summary>
    CriticalChance,
    /// <summary>
    /// 행운
    /// </summary>
    Luck
}

/// <summary>
/// 업그레이드 비용과 강화할 능력치 정보 저장
/// 선태: TargetStat과 ValuePerLevel로 통합 플레이어 스테이트에 반영할 성장효과를 확인
/// 요셉: 현재 업그레이드 정보를 UI에 표시
/// </summary>
public class UpgradeData : ScriptableObject
{
    #region 속성
    public string UpgradeId => _upgradeId;
    public string DisplayName => _displayName;
    public EUpgradeStat TargetStat => _targetStat;
    public int Cost => _cost;
    public float ValuePerLevel => _valuePerLevel;
    public int MaxLevel => _maxLevel;
    #endregion

    #region 인스펙터
    [SerializeField] private string _upgradeId;
    [SerializeField] private string _displayName;
    [SerializeField] private EUpgradeStat _targetStat;
    [SerializeField, Min(0)] private int _cost = 100;
    [SerializeField] private float _valuePerLevel = 1.0f;
    [SerializeField, Min(1)] private int _maxLevel = 10; 
    #endregion
}

