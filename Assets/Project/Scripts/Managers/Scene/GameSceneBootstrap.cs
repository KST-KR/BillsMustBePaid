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

        if (launchMode == GameLaunchData.ELaunchMode.NewGame)
        {
            StartNewGame();
        }
        else
        {
            LoadSavedGame(slotIndex);
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
    /// 저장 데이터를 불러와 런과 스테이지 상태를 복원
    /// </summary>
    private void LoadSavedGame(int slotIndex)
    {
        SaveSlotData saveData = SaveManager.Instance.LoadGame(slotIndex);

        if (saveData == null)
        {
            Debug.LogError($"저장 슬롯 {slotIndex}의 데이터를 불러오지 못했습니다.");
            return;
        }

        if (!saveData.runData.isRunActive ||
            saveData.runData.currentStage <= 0)
        {
            Debug.LogError("이어하기에 필요한 런 또는 스테이지 정보가 올바르지 않습니다.");
            return;
        }

        // 런 활성 상태 복원. OnRunStarted는 발생하지 않습니다.
        GameManager.Instance.RestoreRunState();

        // 스테이지 번호와 결과 화면 상태 복원
        StageManager.Instance.RestoreStageClear(saveData.runData.currentStage);
    }
}