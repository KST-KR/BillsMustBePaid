using System;
using UnityEngine;

/// <summary>
/// 유물 능력치 관리
/// 선태: 유물 획득/초기화 이벤트로 통합 플레이어 상태 갱신
/// 요셉: 유물 목록 획득/초기화 이벤트로 성장/보상 UI 갱신
/// </summary>
public class RelicManager : MonoBehaviour
{
    #region 싱글톤
    public static RelicManager Instance { get; private set; }
    #endregion

    #region 이벤트
    public event Action<RelicData> OnRelicAdded;
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

    #region 커스텀 (Public)
    /// <summary>
    /// 획득한 유물을 런 목록에 추가
    /// 요셉: 보상 선택 결과로 선택된 유물을 전달
    /// 선태: 유물 효과를 통합 플레이어 스테이트에 반영 (OnRelicAdded 제공)
    /// </summary>
    public bool TryAddRelic(RelicData relic)
    {
        return true;
    }

    /// <summary>
    /// 현재 런에서 지정 능력치에 적용되는 유물 효과를 합산
    /// 선태: 통합 플레이어 스테이트 계산 시 유물 보너스를 조회
    /// </summary>
    public float GetStatBonus(EUpgradeStat stat)
    {
        return 0.0f;
    }

    /// <summary>
    /// 런 종료 시 보유 유물을 모두 제거
    /// 선태: 유물 효과가 플레이어 스테이트에서 빠지도록 OnRelicsReset을 처리
    /// 요셉: 런/파산 종료 흐름에서 초기화 시점을 결정
    /// </summary>
    public void ResetRunRelics()
    {

    } 
    #endregion
}
