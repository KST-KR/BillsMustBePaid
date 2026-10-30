using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 씬 진입 시 새 게임과 이어하기를 구분하여 초기화
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 다른 컴포넌트의 Start와 이벤트 구독이 끝날 때까지 대기
        yield return null;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager를 찾을 수 없음.");
            yield break;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager를 찾을 수 없음.");
            yield break;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            yield break;
        }

        int slotIndex = GameLaunchData.SlotIndex;
        GameLaunchData.ELaunchMode launchMode = GameLaunchData.LaunchMode;

        if (slotIndex < 1 || slotIndex > 3)
        {
            Debug.LogError("게임 실행에 필요한 저장 슬롯 정보가 없음.");
            yield break;
        }

        switch (launchMode)
        {
            case GameLaunchData.ELaunchMode.NewGame:
                StartNewGame();
                break;

            case GameLaunchData.ELaunchMode.LoadGame:
                LoadSavedGame(slotIndex);
                break;

            case GameLaunchData.ELaunchMode.OpenSkillTree:
                // TODO : 스클트리 버튼 클릭시 불러와야할 데이터와 상태들
                // LoadSavedGameForSkillTree(slotIndex);
                break;
        }

        GameLaunchData.Clear();
    }

    /// <summary>
    /// 새로운 런을 시작합니다.
    /// </summary>
    private void StartNewGame()
    {
        GameManager.Instance.StartRun();

        // StageManager가 OnRunStarted를 구독하고 있으므로
        // StartRun에서 첫 번째 스테이지가 자동으로 시작됨.
    }

    /// <summary>
    /// 저장 데이터를 검증한 뒤 게임 상태를 순서대로 복원합니다.
    /// </summary>
    private void LoadSavedGame(int slotIndex)
    {
        SaveManager saveManager = SaveManager.Instance;
        CurrencyManager currencyManager = CurrencyManager.Instance;
        GrowthManager growthManager = GrowthManager.Instance;

        if (saveManager == null || currencyManager == null || growthManager == null || GameManager.Instance == null || StageManager.Instance == null)
        {
            Debug.LogError("게임 데이터 복원에 필요한 매니저가 없음.");
            return;
        }

        // 1. 저장 데이터 읽기 (아직 복원 이벤트는 발생하지 않음)
        SaveSlotData saveData = saveManager.LoadGame(slotIndex);

        if (saveData == null)
        {
            Debug.LogError($"저장 슬롯 {slotIndex}의 데이터를 불러오지 못했습니다.");
            return;
        }

        // 2. 런 및 스테이지 데이터 검증
        if (!saveData.runData.isRunActive || saveData.runData.currentStage <= 0)
        {
            Debug.LogError("이어하기에 필요한 런 또는 스테이지 정보가 올바르지 않음.");
            return;
        }

        // 3. 재화 복원
        if (!currencyManager.RestoreBalances(saveData.runData.currentGold, saveData.playerData.legacyPoints))
        {
            Debug.LogError("재화 데이터 복원에 실패");
            return;
        }

        // 4. 성장 시스템에 검증된 저장 데이터 전달
        // GrowthManager가 OnGameLoaded를 구독하고 있으므로
        // 이 시점에 런 성장 데이터 복원이 실행
        saveManager.NotifyGameLoaded(saveData);

        // 5. 런 활성 상태 복원
        GameManager.Instance.RestoreRunState();

        // 6. 저장 당시 스테이지 상태에 따라 복원
        if (saveData.runData.isStageInProgress)
        {
            // 진행 중이던 스테이지를 처음부터 다시 시작
            StageManager.Instance.StartStage(saveData.runData.currentStage);

            Debug.Log($"진행 중이던 스테이지 재시작: {saveData.runData.currentStage}");
        }
        else
        {
            // 클리어 후 결과 화면 상태 복원
            StageManager.Instance.RestoreStageClear(saveData.runData.currentStage);

            Debug.Log($"스테이지 결과 화면 복원: {saveData.runData.currentStage}");
        }

        Debug.Log($"이어하기 데이터 복원 완료: " + $"스테이지 {saveData.runData.currentStage}, " + $"골드 {currencyManager.Gold}, " + $"레거시 포인트 {currencyManager.LegacyPoints}");
    }
}