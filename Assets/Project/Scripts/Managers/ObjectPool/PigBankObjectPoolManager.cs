using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [OBJECT-02-01] 돼지 저금통 오브젝트 풀 매니저
/// </summary>
public class PigBankObjectPoolManager : Singleton<PigBankObjectPoolManager>
{
    #region 인스펙터
    [Header("등록된 돼지 저금통 리스트")]
    [SerializeField] private PigBankDataBaseSO _pigDatabase;

    [Header("생성 개수")]
    [SerializeField] private int _generationCount = 30;
    #endregion

    #region 내부 변수
    private Dictionary<PigBankGameDataSO, Queue<GameObject>> _poolDistionary = new Dictionary<PigBankGameDataSO, Queue<GameObject>>();
    private Dictionary<PigBankGameDataSO, Transform> _rootDistionary = new Dictionary<PigBankGameDataSO, Transform>();
    private List<GameObject> _activePigs = new List<GameObject>();

    private bool _isInitialized;
    #endregion

    #region 프로퍼티
    public bool IsInitialized => _isInitialized;
    #endregion

    #region 이벤트
    protected override void OnSingletonAwake()
    {
        // Addressables 데이터 로딩 이후 Initialize()에서 풀 생성
        // InitPools();
    }

    #endregion

    #region 커스텀 메소드 (Public)
    public GameObject SpawnPigBank(string pigID, Vector3 position, Quaternion rotation)
    {
        if (!_isInitialized)
        {
            Debug.LogError("PigBankObjectPoolManager가 초기화되지 않았습니다.");
            return null;
        }

        if (!_pigDatabase.TryGet(pigID, out PigBankGameDataSO pigData))
        {
            Debug.LogError($"ID에 해당하는 돼지 데이터를 찾을 수 없습니다 : {pigID}");

            return null;
        }

        if(!_poolDistionary.ContainsKey(pigData))
        {
            Debug.LogError($"해당 돼지 저금통{pigID}의 풀이 생성되지 않았다.");

            return null;
        }

        Queue<GameObject> queue = _poolDistionary[pigData];
        GameObject pig = (queue.Count > 0) ? queue.Dequeue() : Instantiate(pigData.PigPrefab, _rootDistionary[pigData]);

        pig.transform.position = position;
        pig.transform.rotation = rotation;
        pig.transform.SetParent(null);
        pig.SetActive(true);

        if (!_activePigs.Contains(pig))
        {
            _activePigs.Add(pig);
        }

        // 돼지 저금통 체력 초기화
        PigBankHealth health = pig.GetComponent<PigBankHealth>();

        if (health != null)
        {
            health.Initialize(pigData);
            health.ResetHealth();
        }
        else
        {
            Debug.LogWarning($"돼지 프리팹에 PigBankHealth가 없음: {pig.name}", pig);
        }

        // 기존 보상 시스템 초기화
        RandomRewardManager rewardManager = pig.GetComponent<RandomRewardManager>();
        if (rewardManager != null)
        {
            rewardManager.Initialize(pigData);
        }
        else
        {
            Debug.LogWarning($"돼지 프리팹에 RandomRewardManager가 없습니다: {pig.name}");
        }

        return pig;
    }

    public void ReturnPigBank(PigBankGameDataSO pigData, GameObject pig)
    {
        _activePigs.Remove(pig);

        if (!_poolDistionary.ContainsKey(pigData))
        {
            return;
        }

        HitEffect hitEffect = pig.GetComponent<HitEffect>();
        if (hitEffect != null)
        {
            hitEffect.RestoreColor();
        }
        else
        {
            Debug.LogError("돼지 저금통에 HitEffect가 없습니다!", this);
        }

        pig.SetActive(false);
        pig.transform.SetParent(_rootDistionary[pigData]);
        _poolDistionary[pigData].Enqueue(pig);
    }

    /// <summary>
    /// Addressables에서 로딩한 저금통 데이터베이스 적용 및 오브젝트 풀 생성
    /// </summary>
    public void Initialize(PigBankDataBaseSO database)
    {
        if (_isInitialized)
        {
            return;
        }

        if (_generationCount <= 0)
        {
            Debug.LogError("PigBankObjectPoolManager: 생성 개수는 1 이상이어야 합니다.");
            return;
        }

        if (database == null)
        {
            Debug.LogError("PigBankObjectPoolManager: PigBankDataBaseSO가 전달되지 않았습니다.");
            return;
        }

        _pigDatabase = database;

        if (!InitPools())
        {
            Debug.LogError("PigBankObjectPoolManager: 오브젝트 풀 생성에 실패했습니다.");

            ClearPools();
            _pigDatabase = null;

            return;
        }

        _isInitialized = true;

        Debug.Log("PigBankObjectPoolManager 초기화 완료");
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private bool InitPools()
    {
        if (_pigDatabase == null || _pigDatabase.Datas == null || _pigDatabase.Datas.Count == 0)
        {
            Debug.LogError("PigBankDataBaseSO 또는 Datas가 존재하지 않습니다.");
            return false;
        }

        foreach (var pigData in _pigDatabase.Datas)
        {
            if (pigData == null || pigData.PigPrefab == null)
            {
                Debug.LogError("PigData가 없거나 PigPrefab이 존재하지 않습니다.");
                return false;
            }

            if (_poolDistionary.ContainsKey(pigData))
            {
                Debug.LogError($"중복된 PigData가 등록되어 있습니다: {pigData.ID}");
                return false;
            }

            GameObject rootObj = new GameObject($"PoolRoot_{pigData.ID}");
            rootObj.transform.SetParent(transform);
            _rootDistionary[pigData] = rootObj.transform;

            Queue<GameObject> queue = new Queue<GameObject>();

            for (int i = 0; i < _generationCount; i++)
            {
                GameObject pig = Instantiate(pigData.PigPrefab, rootObj.transform);
                pig.SetActive(false);
                queue.Enqueue(pig);
            }

            _poolDistionary[pigData] = queue;
        }

        return true;
    }

    private void ClearPools()
    {
        foreach (Transform root in _rootDistionary.Values)
        {
            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }

        _poolDistionary.Clear();
        _rootDistionary.Clear();
        _activePigs.Clear();
    }
    #endregion

}
