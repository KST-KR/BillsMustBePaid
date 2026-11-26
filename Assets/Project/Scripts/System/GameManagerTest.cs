using UnityEngine;

/// <summary>
/// GameManager의 게임 진행 기능을 테스트합니다.
/// </summary>
public class GameManagerTest : MonoBehaviour
{
    #region 이벤트 함수

    /// <summary>
    /// 키보드 입력으로 게임 상태를 테스트합니다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GameManager.Instance.StartRun();
            Debug.Log("런 시작");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GameManager.Instance.GameOver();
            Debug.Log("게임 오버");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log($"현재 상태: {GameManager.Instance.State}");
            Debug.Log($"런 진행 여부: {GameManager.Instance.IsRunActive}");
        }
    }

    #endregion
}
