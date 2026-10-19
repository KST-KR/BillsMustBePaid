using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsTest : MonoBehaviour
{
    #region 인스펙터
    [Header("스텟 테스트")]
    [SerializeField] private PlayerStats _playerStats;
    #endregion

    #region 이벤트
    void Start()
    {
        if (_playerStats == null)
        {
            Debug.LogError("PlayerStats가 연결되지 않음.");
            return;
        }

        // 기본 능력치 확인
        Debug.Log($"기본 공격력 : {_playerStats.AttackPower}");
        Debug.Log($"기본 공격 간격 : {_playerStats.AttackInterval}");

        // 성장 적용
        _playerStats.AddAttackPowerGrowth(5f);

        _playerStats.OnStateChanged += HandleStateChanged;

        Debug.Log($"성장 후 공격력 : {_playerStats.AttackPower}");

        // 성장값 초기화
        _playerStats.ResetRunGrowth();
        Debug.Log($"초기화 후 공격력 : {_playerStats.AttackPower}");
    }

    private void OnDestroy()
    {
        if (_playerStats != null)
        {
            _playerStats.OnStateChanged -= HandleStateChanged;
        }
    }
    #endregion

    #region 커스텀 (private)
    private void HandleStateChanged()
    {
        Debug.Log("능력치가 변경됨.");
    }
    #endregion

}
