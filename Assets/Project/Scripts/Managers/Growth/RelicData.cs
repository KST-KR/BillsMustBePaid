using UnityEngine;

/// <summary>
/// 유물 정보 데이터를 저장
/// 선태: TargetStat과 StatBonus로 통합 플레이어 스테이트에 반영할 유물 효과를 확인
/// 요셉: DisplayName을 유물/보상 UI에 표시
/// </summary>
public class RelicData
{
    #region 속성
    public string RelicId => _relicId;
    public string DisplayName => _displayName;
    public EUpgradeStat TargetStat => _targetStat;
    public float StatBonus => _statBonus;
    #endregion

    #region 인스펙터
    [SerializeField] private string _relicId;
    [SerializeField] private string _displayName;
    [SerializeField] private EUpgradeStat _targetStat;
    [SerializeField] private float _statBonus = 1.0f; 
    #endregion

    // 영구 여부
}