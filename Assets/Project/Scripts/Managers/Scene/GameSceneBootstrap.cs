using UnityEngine;

/// <summary>
/// 게임 씬 진입시 런과 첫 스테이지를 시작
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager를 찾을 수 없음.");
            return;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager를 찾을 수 없음.");
            return;
        }

        if (!GameManager.Instance.IsRunActive)
        {
            GameManager.Instance.StartRun();
        }

        if (!StageManager.Instance.IsStageActive)
        {
            StageManager.Instance.StartStage(1);
        }
    }

}
