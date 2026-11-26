using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 현재 단계에서 저금통 유형에 해당하는 풀만 유효하게 유지
/// </summary>
public class PigBankObjectPoolManager : Singleton<PigBankObjectPoolManager>
{
    private PigBankDataBaseSO _pigDatabase;
    private readonly Dictionary<PigBankGameDataSO, Queue<GameObject>> _poolDictionary = new Dictionary<PigBankGameDataSO, Queue<GameObject>>();
    private readonly Dictionary<PigBankGameDataSO, Transform> _rootDictionary = new Dictionary<PigBankGameDataSO, Transform>();
    private readonly Dictionary<PigBankGameDataSO, GameObject> _prefabDictionary = new Dictionary<PigBankGameDataSO, GameObject>();
    private readonly Dictionary<PigBankGameDataSO, AsyncOperationHandle<GameObject>> _prefabHandles = new Dictionary<PigBankGameDataSO, AsyncOperationHandle<GameObject>>();
    private readonly List<GameObject> _activePigs = new List<GameObject>();
    private bool _isPreparingPools;

    public bool IsInitialized => _pigDatabase != null;

    public void Initialize(PigBankDataBaseSO database)
    {
        if (database == null || database.Datas == null || database.Datas.Count == 0)
        {
            Debug.LogError("돼지 저금통 데이터베이스가 없거나 비어 있음.", this);

            return;
        }

        _pigDatabase = database;
    }

    public bool HasPool(PigBankGameDataSO pigData)
    {
        return pigData != null && _poolDictionary.ContainsKey(pigData);
    }

    public IEnumerator PreparePoolsAsync(IReadOnlyList<PigBankGameDataSO> requestedPigData, int prewarmCountPerType, Action<float> onProgress = null)
    {
        while (_isPreparingPools)
        {
            yield return null;
        }

        if (_pigDatabase == null || requestedPigData == null || prewarmCountPerType <= 0)
        {
            Debug.LogError("Pig bank pool이 구성되어 있지 않거나, 요청한 데이터가 누락", this);

            yield break;
        }

        _isPreparingPools = true;

        HashSet<PigBankGameDataSO> requested = new HashSet<PigBankGameDataSO>();
        for (int i = 0; i < requestedPigData.Count; i++)
        {
            if (requestedPigData[i] != null)
            {
                requested.Add(requestedPigData[i]);
            }
        }

        List<PigBankGameDataSO> existing = new List<PigBankGameDataSO>(_poolDictionary.Keys);
        for (int i = 0; i < existing.Count; i++)
        {
            if (!requested.Contains(existing[i]))
            {
                yield return ReleasePoolAsync(existing[i]);
            }
        }

        int completedTypes = 0;
        int totalTypes = requested.Count;
        foreach (PigBankGameDataSO pigData in requested)
        {
            if (_poolDictionary.ContainsKey(pigData))
            {
                completedTypes++;
                onProgress?.Invoke(totalTypes == 0 ? 1f : (float)completedTypes / totalTypes);

                continue;
            }

            GameObject prefab = pigData.PigPrefab;
            AssetReferenceGameObject prefabReference = pigData.PigPrefabReference;
            AsyncOperationHandle<GameObject> handle = default;

            if (prefabReference != null && prefabReference.RuntimeKeyIsValid())
            {
                handle = Addressables.LoadAssetAsync<GameObject>(prefabReference);
                _prefabHandles[pigData] = handle;

                while (!handle.IsDone)
                {
                    float typeProgress = handle.PercentComplete * 0.8f;
                    onProgress?.Invoke(totalTypes == 0 ? 1f : (completedTypes + typeProgress) / totalTypes);

                    yield return null;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                    _prefabHandles.Remove(pigData);

                    Debug.LogError($"돼지 프리팹을 불러오지 못함: {pigData.ID}", this);
                    _isPreparingPools = false;

                    yield break;
                }

                prefab = handle.Result;
            }

            if (prefab == null)
            {
                Debug.LogError($"돼지 데이터에 할당된 프리팹이 없음: {pigData.ID}", this);
                _isPreparingPools = false;

                yield break;
            }

            _prefabDictionary[pigData] = prefab;
            GameObject root = new GameObject($"PoolRoot_{pigData.ID}");
            root.transform.SetParent(transform);
            _rootDictionary[pigData] = root.transform;

            Queue<GameObject> queue = new Queue<GameObject>();

            for (int i = 0; i < prewarmCountPerType; i++)
            {
                GameObject pig = Instantiate(prefab, root.transform);
                pig.SetActive(false);
                queue.Enqueue(pig);
            }

            _poolDictionary[pigData] = queue;
            completedTypes++;
            onProgress?.Invoke(totalTypes == 0 ? 1f : (float)completedTypes / totalTypes);
        }

        _isPreparingPools = false;
    }

    public GameObject SpawnPigBank(string pigID, Vector3 position, Quaternion rotation)
    {
        if (_pigDatabase == null || !_pigDatabase.TryGet(pigID, out PigBankGameDataSO pigData))
        {
            Debug.LogError($"ID에 해당하는 돼지 데이터를 찾을 수 없음: {pigID}");
            return null;
        }

        if (!_poolDictionary.TryGetValue(pigData, out Queue<GameObject> queue))
        {
            Debug.LogError($"ID에 대한 저금통 풀(piggy bank pool)이 준비되지 않음: {pigID}");
            return null;
        }

        GameObject pig = queue.Count > 0
            ? queue.Dequeue()
            : Instantiate(_prefabDictionary[pigData], _rootDictionary[pigData]);

        pig.transform.SetParent(null);
        pig.transform.SetPositionAndRotation(position, rotation);
        pig.SetActive(true);

        if (!_activePigs.Contains(pig))
        {
            _activePigs.Add(pig);
        }

        PigBankHealth health = pig.GetComponent<PigBankHealth>();
        if (health != null)
        {
            health.Initialize(pigData);
            health.ResetHealth();
        }
        else
        {
            Debug.LogWarning($"돼지 프리팹에 PigBankHealth가 누락: {pig.name}", pig);
        }

        RandomRewardManager rewardManager = pig.GetComponent<RandomRewardManager>();
        if (rewardManager != null)
        {
            rewardManager.Initialize(pigData);
        }
        else
        {
            Debug.LogWarning($"돼지 프리팹에 RandomRewardManager가 누락: {pig.name}", pig);
        }

        return pig;
    }

    public void ReturnPigBank(PigBankGameDataSO pigData, GameObject pig)
    {
        if (pig == null)
        {
            return;
        }

        _activePigs.Remove(pig);

        if (pigData == null || !_poolDictionary.TryGetValue(pigData, out Queue<GameObject> queue))
        {
            Destroy(pig);

            return;
        }

        HitEffect hitEffect = pig.GetComponent<HitEffect>();
        if (hitEffect != null)
        {
            hitEffect.RestoreColor();
        }
        else
        {
            Debug.LogError("돼지 프리팹에 HitEffect가 누락", this);
        }

        pig.SetActive(false);
        pig.transform.SetParent(_rootDictionary[pigData]);
        queue.Enqueue(pig);
    }

    private void OnDestroy()
    {
        ClearAllPools();
    }

    private void ReleasePool(PigBankGameDataSO pigData)
    {
        for (int i = _activePigs.Count - 1; i >= 0; i--)
        {
            GameObject pig = _activePigs[i];
            if (pig == null)
            {
                _activePigs.RemoveAt(i);

                continue;
            }

            PigBankHealth health = pig.GetComponent<PigBankHealth>();
            if (health != null && health.GameData == pigData)
            {
                ReturnPigBank(pigData, pig);
            }
        }

        if (_rootDictionary.TryGetValue(pigData, out Transform root) && root != null)
        {
            Destroy(root.gameObject);
        }

        _poolDictionary.Remove(pigData);
        _rootDictionary.Remove(pigData);
        _prefabDictionary.Remove(pigData);

        if (_prefabHandles.TryGetValue(pigData, out AsyncOperationHandle<GameObject> handle))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            _prefabHandles.Remove(pigData);
        }
    }

    private IEnumerator ReleasePoolAsync(PigBankGameDataSO pigData)
    {
        for (int i = _activePigs.Count - 1; i >= 0; i--)
        {
            GameObject pig = _activePigs[i];
            if (pig == null)
            {
                _activePigs.RemoveAt(i);

                continue;
            }

            PigBankHealth health = pig.GetComponent<PigBankHealth>();
            if (health != null && health.GameData == pigData)
            {
                ReturnPigBank(pigData, pig);
            }
        }

        if (_rootDictionary.TryGetValue(pigData, out Transform root) && root != null)
        {
            Destroy(root.gameObject);
        }

        _poolDictionary.Remove(pigData);
        _rootDictionary.Remove(pigData);
        _prefabDictionary.Remove(pigData);

        AsyncOperationHandle<GameObject> handle = default;
        bool hasHandle = _prefabHandles.TryGetValue(pigData, out handle);
        _prefabHandles.Remove(pigData);

        // 번들을 해제하기 전에, Unity가 인스턴스화된 풀 객체들을 파괴하도록 허용
        yield return null;

        if (hasHandle && handle.IsValid())
        {
            Addressables.Release(handle);
        }
    }

    private void ClearAllPools()
    {
        for (int i = 0; i < _activePigs.Count; i++)
        {
            if (_activePigs[i] != null)
            {
                Destroy(_activePigs[i]);
            }
        }

        List<PigBankGameDataSO> pigData = new List<PigBankGameDataSO>(_poolDictionary.Keys);

        for (int i = 0; i < pigData.Count; i++)
        {
            ReleasePool(pigData[i]);
        }

        _activePigs.Clear();
    }
}
