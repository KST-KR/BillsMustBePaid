using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 단계마다 대상이 되는 저금통들을 생성하고, 해당 저금통들의 풀(pool)만을 준비
/// </summary>
public class SpawnPigBankManager : Singleton<SpawnPigBankManager>
{
    [Header("Pig bank database")]
    [SerializeField] private PigBankDataBaseSO _pigDatabase;

    [Header("Stage spawn settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _baseSpawnCount = 5;
    [SerializeField] private int _stageStep = 2;
    [SerializeField] private int _addCountPerStep = 1;
    [SerializeField] private int _maxSpawnCount = 15;
    [SerializeField] private float _respawnTime = 3f;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(4f, 3f);

    private readonly List<GameObject> _spawnedPigBanks = new List<GameObject>();
    private float _currentRespawnTimer;
    private int _stageSpawnUnlockLevel = 1;
    private bool _isInitialized;
    private bool _isPreparingStagePools;

    public bool IsInitialized => _isInitialized;

    private void Start()
    {
        StartCoroutine(WaitForInitialization());
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            SubscribeStageEvents();
        }
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

    private void OnDrawGizmos()
    {
        if (_spawnPoint == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawCube(_spawnPoint.position, new Vector3(_spawnAreaSize.x, 0.1f, _spawnAreaSize.y));
    }

    public void RegisterPigBank(GameObject pig)
    {
        if (pig != null && !_spawnedPigBanks.Contains(pig))
        {
            _spawnedPigBanks.Add(pig);
        }
    }

    public void Initialize(PigBankDataBaseSO database)
    {
        if (_isInitialized)
        {
            return;
        }

        if (database == null)
        {
            Debug.LogError("SpawnPigBankManager: 돼지 저금통 데이터베이스가 없음", this);
            return;
        }

        _pigDatabase = database;

        if (!SubscribeStageEvents())
        {
            _pigDatabase = null;
            return;
        }

        _isInitialized = true;
        Debug.Log("SpawnPigBankManager가 초기화");
    }

    private IEnumerator WaitForInitialization()
    {
        while (!_isInitialized)
        {
            yield return null;
        }

        if (_spawnPoint == null)
        {
            Debug.LogError("스폰 포인트가 지정되지 않음.", this);
            yield break;
        }

        StartCoroutine(CheckPigBankRoutine());
    }

    private int GetTargetSpawnCount(int stageNumber)
    {
        int step = Mathf.Max(1, _stageStep);
        int count = _baseSpawnCount + ((stageNumber - 1) / step) * _addCountPerStep;

        return Mathf.Min(count, _maxSpawnCount);
    }

    private void HandleStageStarted(int stageNumber)
    {
        _stageSpawnUnlockLevel = PigBankUnlock.Instance != null ? PigBankUnlock.Instance.UnlockLevel : 1;

        _isPreparingStagePools = true;
        ReturnToPool();
        StartCoroutine(PrepareAndSpawnForStage(stageNumber));
    }

    public IEnumerator PrepareStagePoolsAsync(int stageNumber, System.Action<float> onProgress = null)
    {
        if (!_isInitialized || _pigDatabase == null)
        {
            Debug.LogError("SpawnPigBankManager가 초기화되지 않음.", this);

            yield break;
        }

        _stageSpawnUnlockLevel = PigBankUnlock.Instance != null ? PigBankUnlock.Instance.UnlockLevel : 1;
        List<PigBankGameDataSO> eligiblePigs = GetAvailablePigData();
        if (eligiblePigs.Count == 0)
        {
            Debug.LogError("저금통 데이터는 초기 단계의 대상이 아님.", this);

            yield break;
        }

        int prewarmCountPerType = Mathf.Max(1, Mathf.CeilToInt((float)GetTargetSpawnCount(stageNumber) / eligiblePigs.Count));

        yield return PigBankObjectPoolManager.Instance.PreparePoolsAsync(eligiblePigs, prewarmCountPerType, onProgress);
    }

    private IEnumerator PrepareAndSpawnForStage(int stageNumber)
    {
        PigBankObjectPoolManager pool = PigBankObjectPoolManager.Instance;
        if (pool == null || _pigDatabase == null)
        {
            Debug.LogError("단계별 돼지 풀(pig pool)을 준비할 수 없음.", this);

            _isPreparingStagePools = false;
            yield break;
        }

        List<PigBankGameDataSO> eligiblePigs = GetAvailablePigData();
        if (eligiblePigs.Count == 0)
        {
            Debug.LogError("이 단계에 해당하는 저금통 데이터를 찾을 수 없음.", this);

            _isPreparingStagePools = false;
            yield break;
        }

        int prewarmCountPerType = Mathf.Max(1, Mathf.CeilToInt((float)GetTargetSpawnCount(stageNumber) / eligiblePigs.Count));

        yield return pool.PreparePoolsAsync(eligiblePigs, prewarmCountPerType);

        StageManager stageManager = StageManager.Instance;

        if (stageManager == null || !stageManager.IsStageActive || stageManager.CurrentStage != stageNumber)
        {
            _isPreparingStagePools = false;
            yield break;
        }

        for (int i = 0; i < eligiblePigs.Count; i++)
        {
            if (!pool.HasPool(eligiblePigs[i]))
            {
                Debug.LogError($"풀(pool) 준비 실패: {eligiblePigs[i].ID}.", this);

                _isPreparingStagePools = false;
                yield break;
            }
        }

        SpawnPigForStage(stageNumber);
        _isPreparingStagePools = false;
    }

    private void HandleStageCleared(int stageNumber)
    {
        _isPreparingStagePools = false;
        ReturnToPool();
    }

    private void HandleStageFailed()
    {
        _isPreparingStagePools = false;
        ReturnToPool();
    }

    private void SpawnPigForStage(int stageNumber)
    {
        int targetCount = GetTargetSpawnCount(stageNumber);
        PigBankObjectPoolManager pool = PigBankObjectPoolManager.Instance;

        if (pool == null)
        {
            Debug.LogError("PigBankObjectPoolManager가 없음.", this);

            return;
        }

        for (int i = 0; i < targetCount; i++)
        {
            string pigID = GetRandomPigID();
            Vector3 position = RandomSpawnPoint();
            GameObject pig = pool.SpawnPigBank(pigID, position, Quaternion.identity);

            if (pig != null)
            {
                _spawnedPigBanks.Add(pig);
            }
        }

        Debug.Log($"스테이지 {stageNumber}를 위해 돼지 저금통 {targetCount}개를 생성");
    }

    private IEnumerator CheckPigBankRoutine()
    {
        const float checkInterval = 0.5f;

        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            _spawnedPigBanks.RemoveAll(pig => pig == null || !pig.activeInHierarchy);

            if (_isPreparingStagePools)
            {
                _currentRespawnTimer = 0f;
                continue;
            }

            if (_spawnedPigBanks.Count == 0)
            {
                if (StageManager.Instance != null && StageManager.Instance.IsStageActive)
                {
                    _currentRespawnTimer += checkInterval;
                    if (_currentRespawnTimer >= _respawnTime)
                    {
                        RespawnSinglePig();
                        _currentRespawnTimer = 0f;
                    }
                }
            }
            else
            {
                _currentRespawnTimer = 0f;
            }
        }
    }

    private void RespawnSinglePig()
    {
        string pigID = GetRandomPigID();
        if (PigBankObjectPoolManager.Instance == null)
        {
            return;
        }

        GameObject pig = PigBankObjectPoolManager.Instance.SpawnPigBank(pigID, RandomSpawnPoint(), Quaternion.identity);

        if (pig != null)
        {
            _spawnedPigBanks.Add(pig);
        }
    }

    private Vector3 RandomSpawnPoint()
    {
        Vector3 center = _spawnPoint != null ? _spawnPoint.position : transform.position;
        float x = Random.Range(-_spawnAreaSize.x * 0.5f, _spawnAreaSize.x * 0.5f);
        float z = Random.Range(-_spawnAreaSize.y * 0.5f, _spawnAreaSize.y * 0.5f);

        return new Vector3(center.x + x, center.y, center.z + z);
    }

    private void ReturnToPool()
    {
        _currentRespawnTimer = 0f;
        PigBankObjectPoolManager pool = PigBankObjectPoolManager.Instance;

        if (pool != null)
        {
            foreach (GameObject pig in _spawnedPigBanks)
            {
                if (pig == null || !pig.activeInHierarchy)
                {
                    continue;
                }

                PigBankHealth health = pig.GetComponent<PigBankHealth>();
                if (health != null && health.GameData != null)
                {
                    pool.ReturnPigBank(health.GameData, pig);
                }
                else
                {
                    Debug.LogWarning("PigBankHealth 또는 GameData가 없으면 돼지를 반환할 수 없음.", pig);
                    pig.SetActive(false);
                }
            }
        }

        _spawnedPigBanks.Clear();
    }

    private List<PigBankGameDataSO> GetAvailablePigData()
    {
        List<PigBankGameDataSO> available = new List<PigBankGameDataSO>();
        if (_pigDatabase == null || _pigDatabase.Datas == null)
        {
            return available;
        }

        foreach (PigBankGameDataSO pigData in _pigDatabase.Datas)
        {
            if (pigData != null && pigData.CrashLevel <= _stageSpawnUnlockLevel)
            {
                available.Add(pigData);
            }
        }

        if (available.Count == 0 && _pigDatabase.TryGet("PigBank_Basic", out PigBankGameDataSO basic))
        {
            available.Add(basic);
        }

        return available;
    }

    private string GetRandomPigID()
    {
        List<PigBankGameDataSO> available = GetAvailablePigData();
        if (available.Count == 0)
        {
            return "PigBank_Basic";
        }

        List<float> weights = new List<float>(available.Count);
        float totalWeight = 0f;
        for (int i = 0; i < available.Count; i++)
        {
            PigBankGameDataSO pigData = available[i];
            float weight = pigData.SpawnProbability;

            if (pigData.ID == "PigBank_Basic")
            {
                weight = Mathf.Max(10f, weight - ((_stageSpawnUnlockLevel - 1) * 15f));
            }

            weight = Mathf.Max(0f, weight);
            weights.Add(weight);
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return available[0].ID;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < available.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                return available[i].ID;
            }
        }

        return available[available.Count - 1].ID;
    }

    private bool SubscribeStageEvents()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("SpawnPigBankManager: StageManager가 없음.", this);

            return false;
        }

        StageManager.Instance.OnStageStarted -= HandleStageStarted;
        StageManager.Instance.OnStageStarted += HandleStageStarted;
        StageManager.Instance.OnStageCleared -= HandleStageCleared;
        StageManager.Instance.OnStageCleared += HandleStageCleared;
        StageManager.Instance.OnStageFailed -= HandleStageFailed;
        StageManager.Instance.OnStageFailed += HandleStageFailed;

        return true;
    }
}
