using System;
using UnityEngine;

/// <summary>
/// 청구 사이클, 이벤트 파산 후처리와 성장 선택 요청을 관리
/// 요셉: GameManager의 런 시작/종료 및 스테이지 흐름에서 이 매니저의 공개 메서드를 호출
/// </summary>
public class RoguelikeManager : MonoBehaviour
{
    #region 싱글톤
    public static RoguelikeManager Instance { get; private set; }
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
    /// 새 런을 시작하고 첫 청구서를 발행
    /// 요셉: GameManager.StartRun 흐름에서 호출해 첫 청구서를 시작
    /// </summary>
    public bool BeginRun()
    {
        return true;
    }

    /// <summary>
    /// 현재 청구서를 납부하고 다음 사이클 청구서 발행
    /// 요셉: 납부 UI 입력에서 호출 다음 사이클 표시를갱신
    /// </summary>
    public bool PayBillAndAdvance()
    {
        return true;
    }

    /// <summary>
    /// 재시작 시 일반 성장을 초기화하고 영구 성장을 유지한 채 새 런을 시작
    /// 요셉: GameManager의 재시작 선택 쳐리 후 호출
    /// </summary>
    public bool RestartAfterBankruptcy()
    {
        return BeginRun();
    }

    /// <summary>
    /// 외부 게임 흐름에서 런 종료를 알리고 미납 청구서를 정리
    /// 요셉: GameOver 흐름에서 호출
    /// </summary>
    public bool EndRun()
    {
        return true;
    }

    /// <summary>
    /// 플레이어 사망 통지를 받아 현재 런을 종료
    /// 선태: 플레이어 통합 스테이트에서 사망 확정 시 호출
    /// 요셉: GameOver 상태/UI로 전환
    /// </summary>
    public void NotifyPlayerDeath()
    {
        EndRun();
    }

    /// <summary>
    /// 스테이지 클리어 시 성장 선택 UI 활성 알림
    /// 요셉: StageManager/GameManager가 클리어를 확인한 뒤 스테이지 번호와 함께 호출        
    /// </summary>
    public void NotifyStageCleared(int stageNumber)
    {

    } 
    #endregion
}

