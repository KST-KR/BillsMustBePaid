using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private int _spawnCount = 3;
    [SerializeField] private float _respawnTime = 3.0f;
    [SerializeField] private float _spawnRadius = 2.0f;
    #endregion

    #region 내부 변수
    private List<GameObject> _spawnedPigBanks = new List<GameObject>();
    private PigBankGameDataSO _pigData;
    private Coroutine _respawnCoroutine;
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

    private void Update()
    {
        // 오브젝트 풀로 돌아가 비활성화된(!activeInHierarchy) 돼지들을 리스트에서 정리합니다.
        _spawnedPigBanks.RemoveAll(pig => pig == null || !pig.activeInHierarchy);

        // 오류 없스리스폰 코루틴이 돌고 있지 않다면 코루틴을 시작합니다.
        if (_spawnedPigBanks.Count == 0 && _respawnCoroutine == null)
        {
            if (StageManager.Instance == null || StageManager.Instance.IsStageActive)
            {
                _respawnCoroutine = StartCoroutine(RespawnRoutine());
            }
        }
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
    /// 일정 시간 대기 후 1마리를 리스폰하는 코루틴
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        Debug.Log("리스폰 시작");
        yield return new WaitForSeconds(_respawnTime);

        if (_pigData != null)
        {
            int currentStage = (StageManager.Instance != null) ? StageManager.Instance.CurrentStage : 1;
            string pigID = GetPigIDForStage(currentStage);
            Vector3 spawnPosition = RandomSpawnPoint();

            if (PigBankObjectPoolManager.Instance != null)
            {
                GameObject pig = PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, spawnPosition, Quaternion.identity);
                if (pig != null)
                {
                    _spawnedPigBanks.Add(pig);
                    Debug.Log($"필드에 돼지가 없어 {_respawnTime}초 뒤 1마리를 리스폰.");
                }
            }
        }

        // 코루틴이 끝났으므로 참조를 초기화하여 다음 0마리 상황에 다시 실행될 수 있개 한다.
        _respawnCoroutine = null;
    }

    /// <summary>
    /// 기준 위치를 중심으로 원형 범위 내의 무작위 3D 좌표를 반환
    /// </summary>
    private Vector3 RandomSpawnPoint()
    {
        Vector3 basePosition = (_spawnPoint != null) ? _spawnPoint.position : transform.position;

        // XZ 평면 상에서 원 내부의 무작위 점 추출 (Random.insideUnitCircle은 2차원 반경 벡터 반환)
        Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;

        // Y축(높이)은 스폰 포인트의 높이를 유지하고, X와 Z에 오프셋을 더해줌
        Vector3 spawnPos = new Vector3(basePosition.x + randomCircle.x, basePosition.y, basePosition.z + randomCircle.y);

        return spawnPos;
    }

    /// <summary>
    /// 현재 필드에 있는 돼지를 오브젝트 풀로 반환합니다.
    /// </summary>
    private void ReturnToPool()
    {
        // 진행 중이던 리스폰 코루틴이 있다면 강제로 중지
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }

        _spawnedPigBanks.RemoveAll(pig => pig == null);

        if (_pigData != null && PigBankObjectPoolManager.Instance != null)
        {
            foreach (var pig in _spawnedPigBanks)
            {
                if (pig != null)
                {
                    PigBankObjectPoolManager.Instance.ReturnPigBank(_pigData, pig);
                }
            }
        }
        _spawnedPigBanks.Clear();
        _pigData = null;
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
