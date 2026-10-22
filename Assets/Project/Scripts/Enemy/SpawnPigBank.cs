using UnityEngine;

/// <summary>
/// 필드위 돼지 저금통 스폰 관리
/// </summary>
public class SpawnPigBank : MonoBehaviour
{
    #region 이벤트
    private void OnEnable()
    {
        StageManager.Instance.OnStageStarted += HandleStageStarted;
    }

    private void Start()
    {
        if(_spawnPoint == null)
        {
            Debug.LogError("SpawnPoint가 존재 하지 않습니다. 확인 필요");
            return;
        }
    }

    private void OnDisable()
    {
        StageManager.Instance.OnStageStarted -= HandleStageStarted;
    }
    #endregion

    #region 인스펙터
    [Header("돼지 저금통 스폰 설정")]
    [SerializeField] private Transform _spawnPoint;
    #endregion

    #region 커스텀 메소드 (Private)
    /// <summary>
    /// 이벤트와 타입을 맞춰주기 위해 int stageNumber를 인자로 받습니다.
    /// </summary>
    private void HandleStageStarted(int stageNumber)
    {
        SpawnPigForStage(stageNumber);
    }

    private void SpawnPigForStage(int stageNumber)
    {
        string pigID = GetPigIDForStage(stageNumber);

        Vector3 spawnPosition = (_spawnPoint != null) ? _spawnPoint.position : transform.position;

        if (PigBankObjectPoolManager.Instance != null)
        {
            PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, spawnPosition, Quaternion.identity);
        }
    }

    private string GetPigIDForStage(int stageNumber)
    {
        switch (stageNumber)
        {
            case 1:
                return "PigBank_Basic";
            default:
                return "PigBank_Basic";
        }
    }
    #endregion
}
