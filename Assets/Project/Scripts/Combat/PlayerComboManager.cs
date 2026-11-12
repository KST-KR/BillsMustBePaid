using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerComboManager : MonoBehaviour
{
    #region 상수
    private const int CComboThreshold = 10;
    private const float CLuckChancePerPoint = 0.01f;
    private const float CMaxDoubleAttackChance = 0.5f;
    #endregion

    #region 내부 변수
    private PlayerStats _playerStats;

    private int _comboCount;
    private int _lastCheckedMilestone;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 누적된 콤보 횟수
    /// </summary>
    public int ComboCount => _comboCount;

    /// <summary>
    /// 행운 능력치에 따른 추가 공격 확률
    /// </summary>
    public float DoubleAttackChance
    {
        get
        {
            return Mathf.Clamp(_playerStats.Luck * CLuckChancePerPoint, 0f, CMaxDoubleAttackChance);
        }
    }
    #endregion

    #region 이벤트
    /// <summary>
    /// 콤보 횟수가 변경되었을 때 발생
    /// </summary>
    public event Action<int> OnComboChanged;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();

        if (_playerStats == null)
        {
            Debug.LogError("PlayerStats를 찾을 수 없음.", this);
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 공격 결과에 따라 콤보를 갱신
    /// </summary>
    /// <param name="isHit">공격 대상 적중 여부</param>
    /// <returns>추가 공격 발동 여부</returns>
    public bool RecordAttackResult(bool isHit)
    {
        // 미적중 시 콤보 초기화
        if (!isHit)
        {
            ResetCombo();
            return false;
        }

        _comboCount++;
        OnComboChanged?.Invoke(_comboCount);

        // 새로운 10회 단위에 도달했는지 확인
        int currentMilestone = _comboCount / CComboThreshold;

        if (currentMilestone <= _lastCheckedMilestone)
        {
            return false;
        }

        _lastCheckedMilestone = currentMilestone;

        // 행운 능력치에 따른 추가 공격 확률 판정
        return UnityEngine.Random.value < DoubleAttackChance;
    }

    /// <summary>
    /// 콤보 초기화
    /// </summary>
    public void ResetCombo()
    {
        _comboCount = 0;
        _lastCheckedMilestone = 0;

        OnComboChanged?.Invoke(_comboCount);
    }
    #endregion
}
