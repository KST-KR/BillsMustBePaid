using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필드위 돼지 저금통 스폰 관리
/// </summary>
public class SpawnPigBankManager : MonoBehaviour
{
    #region 인스펙터
    [Header("데이터 베이스 참조")]
    [SerializeField] private PigBankDataBaseSO _pigDatabase;

    [Header("돼지 저금통 스폰 설정")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _spawnCount = 3;
    [SerializeField] private float _respawnTime = 3.0f;
    [SerializeField] private float _spawnRadius = 2.0f;
    #endregion

    #region 내부 변수
    private List<GameObject> _spawnedPigBanks = new List<GameObject>();
    private PigBankGameDataSO _pigData;
    private float _currentRespawnTimer = 0f;
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

        StartCoroutine(CheckPigBankRoutine());
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted -= HandleStageStarted;
        }
    }

    /// <summary>
    /// 에디터에서 스폰 범위를 그리기 위한 기즈모
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_spawnPoint.position, _spawnRadius);
    }
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
        ReturnToPool();

        string pigID = GetRandomPigID();

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

        if (PigBankObjectPoolManager.Instance != null)
        {
            for (int i = 0; i < _spawnCount; i++)
            {
                Vector3 spawnPosition = RandomSpawnPoint();

                GameObject pig = PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, spawnPosition, Quaternion.identity);
                if (pig != null)
                {
                    _spawnedPigBanks.Add(pig);
                }
            }
            Debug.Log($"성공적으로 {pigID} 돼지를 {_spawnCount}마리 스폰했습니다.");
        }
        else
        {
            Debug.LogError("PigBankObjectPoolManager Instance가 Null입니다!");
        }
    }

    /// <summary>
    /// 일정 주기마다 검사
    /// </summary>
    private IEnumerator CheckPigBankRoutine()
    {
        float checkInterval = 0.5f; // 0.5초 마다 검사

        while (true)
        {
            float timer = 0f;

            while (timer < checkInterval)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // 오브젝트 풀로 돌아가 비활성화된 돼지들을 리스트에서 정리
            _spawnedPigBanks.RemoveAll(pig => pig == null || !pig.activeInHierarchy);

            // 돼지가 모두 사라졌고, 리스폰 코루틴이 돌고 있지 않다면 실행
            if (_spawnedPigBanks.Count == 0)
            {
                if (StageManager.Instance == null || StageManager.Instance.IsStageActive)
                {
                    _currentRespawnTimer += checkInterval;

                    if(_currentRespawnTimer >= _respawnTime)
                    {
                        RespawnSinglePig();
                        _currentRespawnTimer = 0f;
                    }
                }
            }
            else
            {
                // 돼지가 살아있다면 타이머 초기화
                _currentRespawnTimer = 0f;
            }
        }
    }

    private void RespawnSinglePig()
    {
        int currentStage = (StageManager.Instance != null) ? StageManager.Instance.CurrentStage : 1;
        string pigID = GetRandomPigID();

        if (PigBankObjectPoolManager.Instance != null)
        {
            Vector3 spawnPosition = RandomSpawnPoint();
            GameObject pig = PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, spawnPosition, Quaternion.identity);

            if (pig != null)
            {
                _spawnedPigBanks.Add(pig);

                Debug.Log($"필드에 돼지가 없어 1마리를 리스폰했습니다.)");
            }
        }
    }

    /// <summary>
    /// 기준 위치를 중심으로 원형 범위 내의 무작위 3D 좌표를 반환
    /// </summary>
    private Vector3 RandomSpawnPoint()
    {
        Vector3 basePosition = (_spawnPoint != null) ? _spawnPoint.position : transform.position;
        Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
        Vector3 spawnPos = new Vector3(basePosition.x + randomCircle.x, basePosition.y, basePosition.z + randomCircle.y);

        return spawnPos;
    }

    /// <summary>
    /// 현재 필드에 있는 돼지를 오브젝트 풀로 반환합니다.
    /// </summary>
    private void ReturnToPool()
    { 
        _currentRespawnTimer = 0;

        if (_pigData != null && PigBankObjectPoolManager.Instance != null)
        {
            foreach (var pig in _spawnedPigBanks)
            {
                if (pig != null && pig.activeInHierarchy)
                {
                    PigBankObjectPoolManager.Instance.ReturnPigBank(_pigData, pig);
                }
            }
        }

        _spawnedPigBanks.Clear();
        _pigData = null;
    }

    private string GetRandomPigID()
    {
        List<string> availablePigIDs = new List<string>();

        if(_pigDatabase != null && _pigDatabase.Datas != null)
        {
            int currentUnlockLevel = 1;
            currentUnlockLevel = PigBankUnlock.Instance.UnlockLevel;

            foreach(var pigData in _pigDatabase.Datas)
            {
                if(pigData == null)
                {
                    continue;
                }

                if(pigData.CrashLevel <=  currentUnlockLevel)
                {
                    availablePigIDs.Add(pigData.ID);
                }
            }
        }
        // 리스트가 비어있다면 기본 돼지 저금통 반환
        if(availablePigIDs.Count == 0)
        {
            return "PigBank_Basic";
        }

        // 해금된 돼지 저금통 들중 하나를 골라 반환
        int randomIndex = Random.Range(0, availablePigIDs.Count);
        return availablePigIDs[randomIndex];
    }
    #endregion
}
