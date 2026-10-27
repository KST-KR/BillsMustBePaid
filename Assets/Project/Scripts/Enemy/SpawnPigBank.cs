using UnityEngine;

/// <summary>
/// 필드위 돼지 저금통 스폰 관리
/// </summary>
public class SpawnPigBank : MonoBehaviour
{
    #region 인스펙터
    [Header("데이터 베이스 참조")]
    [SerializeField] private PigBankDataBaseSO _pigDatabase;

    [Header("돼지 저금통 스폰 설정")]
    [SerializeField] private Transform _spawnPoint;
    #endregion

    #region 내부 변수
    private GameObject _spawnedPigBank;
    private PigBankGameDataSO _pigData;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += HandleStageStarted;
        }
    }

    private void Start()
    {
        if (_spawnPoint == null)
        {
            Debug.LogError("SpawnPoint가 존재 하지 않습니다. 확인 필요");
            return;
        }

        if (StageManager.Instance == null || !StageManager.Instance.IsStageActive)
        {
            Debug.LogWarning("에디터 단독 테스트 감지: 1스테이지 기준으로 강제 스폰합니다.");
            SpawnPigForStage(1);
        }
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted -= HandleStageStarted;
        }
    }
    #endregion

    #region 커스텀 메소드 (Private)
    /// <summary>
    /// 이벤트와 타입을 맞춰주기 위해 int stageNumber를 인자로 받습니다.
    /// </summary>
    private void HandleStageStarted(int stageNumber)
    {
        ReturnToPool();

        SpawnPigForStage(stageNumber);
    }

    private void SpawnPigForStage(int stageNumber)
    {
        string pigID = GetPigIDForStage(stageNumber);

        if (_pigDatabase != null)
        {
            if (!_pigDatabase.TryGet(pigID, out _pigData))
            {
                Debug.LogError($"데이터베이스에서 ID를 찾지 못했습니다: {pigID}");
                return;
            }
        }
        else
        {
            Debug.LogError("SpawnPigBank에 PigBankDataBaseSO가 연결되지 않았습니다!");
            return;
        }

        Vector3 spawnPosition = (_spawnPoint != null) ? _spawnPoint.position : transform.position;

        if (PigBankObjectPoolManager.Instance != null)
        {
           _spawnedPigBank = PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, spawnPosition, Quaternion.identity);
            Debug.Log($"성공적으로 {pigID} 돼지를 {spawnPosition}에 스폰했습니다");
        }

        else
        {
            Debug.LogError("PigBankObjectPoolManager Instance가 Null입니다!");
        }
    }

    /// <summary>
    /// 현재 필드에 있는 돼지를 오브젝트 풀로 반환합니다.
    /// </summary>
    private void ReturnToPool()
    {
        if(_spawnedPigBank != null && _pigData != null)
        {
            if(PigBankObjectPoolManager.Instance != null)
            {
                PigBankObjectPoolManager.Instance.ReturnPigBank(_pigData, _spawnedPigBank);
            }
            _spawnedPigBank = null;
            _pigData = null;
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
