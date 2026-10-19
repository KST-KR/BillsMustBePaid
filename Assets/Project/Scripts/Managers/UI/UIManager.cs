
using UnityEngine;

/// <summary>
/// 게임 UI의 표시 및 갱신을 관리합니다.
/// 외부 시스템에서 전달받은 정보를 UI에 반영합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 싱글톤

    public static UIManager Instance { get; private set; }

    #endregion

    #region 이벤트 함수

    /// <summary>
    /// UIManager 싱글톤을 등록합니다.
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
    /// UIManager가 파괴될 때 싱글톤 참조를 해제합니다.
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
    /// 타이틀 UI를 표시합니다.
    /// </summary>
    public void ShowTitleUI()
    {
        // TODO: 타이틀 UI 표시
    }

    /// <summary>
    /// 게임 플레이 UI를 표시합니다.
    /// </summary>
    public void ShowGameUI()
    {
        // TODO: 플레이 UI 표시
    }

    /// <summary>
    /// 일시정지 UI를 표시합니다.
    /// </summary>
    public void ShowPauseUI()
    {
        // TODO: 일시정지 UI 표시
    }

    /// <summary>
    /// 스테이지 클리어 UI를 표시합니다.
    /// </summary>
    public void ShowStageClearUI()
    {
        // TODO: 스테이지 클리어 UI 표시
    }

    /// <summary>
    /// 게임 오버 UI를 표시합니다.
    /// </summary>
    public void ShowGameOverUI()
    {
        // TODO: 게임 오버 UI 표시
    }

    /// <summary>
    /// 전체 UI 패널을 숨깁니다.
    /// </summary>
    public void HideAllPanels()
    {
        // TODO: UI 패널 숨김
    }

    /// <summary>
    /// 현재 스태미나와 최대 스태미나를 UI에 반영합니다.
    /// </summary>
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        // TODO: 스태미나 UI 갱신
    }

    /// <summary>
    /// 보유 재화를 UI에 반영합니다.
    /// </summary>
    public void UpdateMoneyUI(int amount)
    {
        // TODO: 재화 UI 갱신
    }

    /// <summary>
    /// 현재 스테이지 번호를 UI에 반영합니다.
    /// </summary>
    public void UpdateStageUI(int stageNumber)
    {
        // TODO: 스테이지 UI 갱신
    }

    /// <summary>
    /// 저금통 파괴에 따른 레벨 수치를 UI에 반영합니다.
    /// </summary>
    public void UpdateLevelUI(int level)
    {
        // TODO: 레벨 UI 갱신
    }

    /// <summary>
    /// 청구서 금액을 UI에 반영합니다.
    /// </summary>
    public void UpdateBillUI(int amount)
    {
        // TODO: 청구서 UI 갱신
    }

    /// <summary>
    /// 성장 및 보상 선택 UI를 표시합니다.
    /// </summary>
    public void ShowRewardUI()
    {
        // TODO: 성장 및 보상 UI 표시
    }

    #endregion
}

