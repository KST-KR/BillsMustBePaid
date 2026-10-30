using System;
using UnityEngine;

/// <summary>
/// 청구 사이클, 이벤트 파산 후처리와 성장 선택 요청을 관리
/// </summary>
public class RoguelikeManager : MonoBehaviour
{
    #region 싱글톤
    public static RoguelikeManager Instance { get; private set; }
    #endregion

    #region 속성
    public int CycleCount { get; private set; } = 1;
    #endregion

    private bool _isRestartingAfterBankruptcy;


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
    /// </summary>
    public bool BeginRun()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsRunActive)
        {
            return false;
        }

        GameManager.Instance.StartRun();
        return GameManager.Instance.IsRunActive;
    }

    /// <summary>
    /// 현재 청구서를 납부하고 다음 사이클 청구서 발행
    /// </summary>
    public bool PayBillAndAdvance()
    {
        return true;
    }

    /// <summary>
    /// 재시작 시 일반 성장을 초기화하고 영구 성장을 유지한 채 새 런을 시작
    /// </summary>
    public bool RestartAfterBankruptcy()
    {
        if (_isRestartingAfterBankruptcy || BillManager.Instance == null || !BillManager.Instance.HasExpiredBills ||
            GameManager.Instance == null || CurrencyManager.Instance == null)
        {
            return false;
        }

        _isRestartingAfterBankruptcy = true;
        CycleCount = CycleCount == int.MaxValue ? int.MaxValue : CycleCount + 1;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveBankruptcyCycle(CycleCount);
        }

        if (GameManager.Instance.IsRunActive)
        {
            GameManager.Instance.EndRun();
        }

        CurrencyManager.Instance.ResetRunGold();
        if (GrowthManager.Instance != null)
        {
            GrowthManager.Instance.ResetRunGrowth();
        }

        BillManager.Instance.ResetRunBills();
        bool restarted = BeginRun();
        if (restarted && GameSaveController.Instance != null)
        {
            GameSaveController.Instance.SaveCurrentProgress(CurrencyManager.Instance.Gold);
        }

        _isRestartingAfterBankruptcy = false;
        return restarted;
    }

    /// <summary>
    /// [ROGUE-02-02] 저장 데이터에서 영구 사이클 번호 복원
    /// 누락되거나 잘못된 값은 첫 사이클로 보정
    /// </summary>
    public void RestoreCycleCount(int cycleCount)
    {
        CycleCount = Mathf.Max(1, cycleCount);
    }

    /// <summary>
    /// 외부 게임 흐름에서 런 종료를 알리고 미납 청구서를 정리
    /// </summary>
    public bool EndRun()
    {
        return true;
    }

    /// <summary>
    /// 플레이어 사망 통지를 받아 현재 런을 종료
    /// </summary>
    public void NotifyPlayerDeath()
    {
        EndRun();
    }

    /// <summary>
    /// 스테이지 클리어 시 성장 선택 UI 활성 알림     
    /// </summary>
    public void NotifyStageCleared(int stageNumber)
    {

    } 
    #endregion
}

