using System;
using UnityEngine;

/// <summary>
/// 스테이지의 시작, 클리어 및 전환을 관리합니다.
/// 게임 전체 상태는 GameManager에서 관리합니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    #region 싱글톤

    public static StageManager Instance { get; private set; }

    #endregion

    #region 속성

    public int CurrentStage => _currentStage;

    #endregion

    #region 이벤트

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action<int> OnStageChanged;
    public event Action OnStageFailed;

    #endregion

    #region 내부 변수

    private int _currentStage;

    #endregion

    #region 이벤트 함수

    /// <summary>
    /// StageManager 싱글톤을 등록합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// StageManager가 파괴될 때 싱글톤 참조를 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 지정한 번호의 스테이지를 시작합니다.
    /// </summary>
    public void StartStage(int stageNumber)
    {
        // TODO: 스테이지 시작 처리
    }

    /// <summary>
    /// 현재 스테이지를 클리어 처리합니다.
    /// </summary>
    public void ClearStage()
    {
        // TODO: 스테이지 클리어 처리
    }

    /// <summary>
    /// 다음 스테이지로 전환합니다.
    /// </summary>
    public void NextStage()
    {
        // TODO: 다음 스테이지 전환 처리
    }

    /// <summary>
    /// 현재 스테이지의 실패를 처리합니다.
    /// </summary>
    public void FailStage()
    {
        // TODO: 스테이지 실패 처리
    }

    /// <summary>
    /// 스테이지 진행 정보를 초기화합니다.
    /// </summary>
    public void ResetStage()
    {
        // TODO: 스테이지 초기화 처리
    }

    #endregion
}

