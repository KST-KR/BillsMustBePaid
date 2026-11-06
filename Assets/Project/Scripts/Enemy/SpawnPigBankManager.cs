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
    private float _currentRespawnTimer = 0f;
    private int _stageSpawnUnlockLevel = 1;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += HandleStageStarted;
            StageManager.Instance.OnStageCleared += HandleStageCleared; 
            StageManager.Instance.OnStageFailed += HandleStageFailed;   
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
            StageManager.Instance.OnStageCleared -= HandleStageCleared; 
            StageManager.Instance.OnStageFailed -= HandleStageFailed;   
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
        if (PigBankUnlock.Instance != null)
        {
            _stageSpawnUnlockLevel = PigBankUnlock.Instance.UnlockLevel; 
        }
        else
        {
            _stageSpawnUnlockLevel = 1;
        }

        SpawnPigForStage(stageNumber);
    }

    private void HandleStageCleared(int stageNumber)
    {
        ReturnToPool();
    }

    private void HandleStageFailed()
    {
        ReturnToPool();
    }

    private void SpawnPigForStage(int stageNumber)
    {
        ReturnToPool();       

        if (PigBankObjectPoolManager.Instance != null)
        {
            for (int i = 0; i < _spawnCount; i++)
            {
                string pigID = GetRandomPigID();

                if (!_pigDatabase.TryGet(pigID, out var spawnedPigData))
                {
                    continue;
                }

                Vector3 spawnPosition = RandomSpawnPoint();
                GameObject pig = PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, spawnPosition, Quaternion.identity);

                if (pig != null)
                {
                    _spawnedPigBanks.Add(pig);
                }
            }
            Debug.Log($"성공적으로 돼지를 {_spawnCount}마리 스폰했습니다.");
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

        if (PigBankObjectPoolManager.Instance != null)
        {
            foreach (var pig in _spawnedPigBanks)
            {
                if (pig != null && pig.activeInHierarchy)
                {
                    PigBankHealth health = pig.GetComponent<PigBankHealth>();
                    if (health != null && health.GameData != null)
                    {
                        PigBankObjectPoolManager.Instance.ReturnPigBank(health.GameData, pig);
                    }
                    else
                    {
                        Debug.LogWarning("반환하려는 돼지에 PigBankHealth 또는 GameData가 없습니다.", pig);
                        pig.SetActive(false);
                    }
                }
            }
        }

        _spawnedPigBanks.Clear();
    }

    private string GetRandomPigID()
    {
        List<PigBankGameDataSO> availablePigs = new List<PigBankGameDataSO>();
        List<float> probability = new List<float>();
        float totalProbability = 0f;

        if (_pigDatabase != null && _pigDatabase.Datas != null)
        {
            int currentUnlockLevel = _stageSpawnUnlockLevel;

            foreach(var pigData in _pigDatabase.Datas)
            {
                if(pigData == null)
                {
                    continue;
                }

                if(pigData.CrashLevel <=  currentUnlockLevel)
                {
                    availablePigs.Add(pigData);

                    float currentProbability = pigData.SpawnProbability;

                    // 레벨이 1오를때마다 스폰확률 15씩 감소 (최소 10은 유지)
                    if (pigData.ID == "PigBank_Basic")
                    {
                        currentProbability = Mathf.Max(10f, pigData.SpawnProbability - ((currentUnlockLevel - 1) * 15f));
                    }

                    probability.Add(currentProbability);
                    totalProbability += currentProbability;
                }
            }
        }
        // 리스트가 비어있다면 기본 돼지 저금통 반환
        if(availablePigs.Count == 0)
        {
            return "PigBank_Basic";
        }

        float randomValue = Random.Range(0f, totalProbability);
        float currentProbabilitySum = 0f;

        for(int i = 0; i < availablePigs.Count; i++)
        {
            currentProbabilitySum += probability[i];

            if(randomValue <= currentProbabilitySum)
            {
                return availablePigs[i].ID;
            }

        }

        return availablePigs[availablePigs.Count - 1].ID;

    }
    #endregion
}
