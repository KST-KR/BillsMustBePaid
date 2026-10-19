using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 성장 구매와 일반 및 영구 성장 데이터를 관리
/// 선태: UpgradeId 정보로 통합 플레이어 스테이트를 갱신
/// 요셉: OnGrowthChanged로 성장 UI를 갱신하고 Export/Import 메서드로 SaveManager와 데이터를 공유
/// </summary>
public class GrowthManager : MonoBehaviour
{
    #region 싱글톤
    public static GrowthManager Instance { get; private set; }
    #endregion

    #region 속성
    /// <summary>
    /// 요셉: 스킬트리 UI에서 표시할 업그레이드 항목 목록으로 사용
    /// </summary>
    public IReadOnlyList<UpgradeData> Upgrades => _upgrades;
    #endregion

    #region 이벤트
    public event Action<string, int, float> OnUpgradePurchased;
    #endregion

    #region 인스펙터
    [Header("업그레이드 데이터")]
    [SerializeField] private UpgradeData[] _upgrades;
    #endregion


    #region 이벤트 함수
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    } 
    #endregion

    /// <summary>
    /// 업그레이드가 현재 재화와 레벨 조건에서 구매 가능한지 확인
    /// 요셉: 스킬트리 노드의 잠금/구매 가능 표시를 갱신
    /// </summary>
    public bool CanPurchase(string upgradeId)
    {
        return true;
    }

    /// <summary>
    /// 재화를 지불할 수 있고 최대레벨 미만일 때 업그레이드를 적용
    /// 요셉: 선택한 UpgradeId로 호출하고 성공 여부를 표시
    /// 선태: OnUpgradePurchased의 UpgradeId/레벨/ 증가량으로 업그레이드 효과를 찾아 스테이트에 반영
    /// </summary>
    public bool TryPurchase(string upgradeId)
    {
        return true;
    }

    /// <summary>
    /// 지정한 업그레이드의 현재 레벨을 반환 (0이면 미습득)
    /// 요셉: 성장 선택 UI에 레벨을 표시
    /// </summary>
    public int GetLevel(string upgradeId)
    {
        return 0;
    }

    /// <summary>
    /// 업그레이드를 한 번 이상 습득했는지 확인
    /// 선태: 플레이어 통합 스테이트에서 습득한 스킬만 별도 효과로 계산할 때 사용
    /// 요셉: 스킬트리 노드의 습득/미습득 표시를 갱신
    /// </summary>
    public bool IsLearned(string upgradeId)
    {
        return GetLevel(upgradeId) > 0;
    }

    /// <summary>
    /// 지정 능력치에서 습득한 업그레이드의 레벨별 증가량만 합산
    /// 선태: 통합 플레이어 스테이트 계산에 사용할 습득 스킬 증가량을 조회
    /// </summary>
    public float GetStatValue(EUpgradeStat stat)
    {
        return 0.0f;
    }

    /// <summary>
    /// 파산 시 성장 초기화
    /// 선태: 일반 성장으로 적용한 플레이어 스테이트를 초기화하고 영구 성장 재적용을 확인
    /// 요셉: 파산/재시작 흐름에서 호출 시점을 전달
    /// </summary>
    public void ResetRunGrowth()
    {

    }
}
