using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public struct SpecialCoinInfo
{
    [Tooltip("특수 코인 종류")]
    public SpecialCoinType _coinType;

    [Tooltip("Addressables 특수 코인 프리팹")]
    public AssetReferenceGameObject _coinPrefabReference;
}

public class SpecialCoinPoolManager : Singleton<SpecialCoinPoolManager>
{
    [Header("특수 코인 프리팹 참조")]
    [SerializeField] private SpecialCoinInfo[] _specialCoinTable;

    private readonly Dictionary<SpecialCoinType, Queue<SpecialCoin>> _coinPools = new Dictionary<SpecialCoinType, Queue<SpecialCoin>>();
    private readonly Dictionary<SpecialCoinType, AssetReferenceGameObject> _referenceLookup = new Dictionary<SpecialCoinType, AssetReferenceGameObject>();
    private readonly Dictionary<SpecialCoinType, AsyncOperationHandle<GameObject>> _prefabHandles = new Dictionary<SpecialCoinType, AsyncOperationHandle<GameObject>>();
    private readonly Dictionary<SpecialCoinType, List<Vector3>> _pendingSpawnPositions = new Dictionary<SpecialCoinType, List<Vector3>>();
    private readonly HashSet<SpecialCoinType> _loadingTypes = new HashSet<SpecialCoinType>();
    private readonly HashSet<SpecialCoin> _activeCoins = new HashSet<SpecialCoin>();
    private readonly List<SpecialCoinType> _availableTypes = new List<SpecialCoinType>();
    private bool _isPreparingPrefabs;
    private bool _prefabsPrepared;

    public bool IsEnabled => SettingsManager.Instance == null || SettingsManager.Instance.CoinEnabled;
    public bool HasAvailablePrefabs => _availableTypes.Count > 0;

    protected override void OnSingletonAwake()
    {
        _specialCoinTable ??= Array.Empty<SpecialCoinInfo>();
        InitializePools();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }

        StopAllCoroutines();

        foreach (AsyncOperationHandle<GameObject> handle in _prefabHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _prefabHandles.Clear();
        _loadingTypes.Clear();
        _pendingSpawnPositions.Clear();
    }

    private void InitializePools()
    {
        _coinPools.Clear();
        _referenceLookup.Clear();
        _availableTypes.Clear();

        foreach (SpecialCoinType type in Enum.GetValues(typeof(SpecialCoinType)))
            _coinPools[type] = new Queue<SpecialCoin>();

        for (int i = 0; i < _specialCoinTable.Length; i++)
        {
            SpecialCoinInfo info = _specialCoinTable[i];

            if (info._coinPrefabReference == null || !info._coinPrefabReference.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"특수 코인 Addressables 참조가 없습니다: {info._coinType}", this);

                continue;
            }

            _referenceLookup[info._coinType] = info._coinPrefabReference;

            if (!_availableTypes.Contains(info._coinType))
            {
                _availableTypes.Add(info._coinType);
            }
        }
    }

    private void HandleSettingsChanged()
    {
        if (IsEnabled)
        {
            return;
        }

        foreach (SpecialCoin coin in _activeCoins)
        {
            if (coin != null)
            {
                Destroy(coin.gameObject);
            }
        }
        _activeCoins.Clear();

        foreach (Queue<SpecialCoin> pool in _coinPools.Values)
        {
            foreach (SpecialCoin coin in pool)
            {
                if (coin != null)
                {
                    Destroy(coin.gameObject);
                }
            }

            pool.Clear();
        }

        _pendingSpawnPositions.Clear();
        ReleaseLoadedPrefabs();
        _prefabsPrepared = false;
    }

    private void ReleaseLoadedPrefabs()
    {
        List<SpecialCoinType> releaseTypes = new List<SpecialCoinType>();

        foreach (KeyValuePair<SpecialCoinType, AsyncOperationHandle<GameObject>> pair in _prefabHandles)
        {
            if (_loadingTypes.Contains(pair.Key))
            {
                continue;
            }

            if (pair.Value.IsValid())
            {
                Addressables.Release(pair.Value);
            }
            releaseTypes.Add(pair.Key);
        }

        for (int i = 0; i < releaseTypes.Count; i++)
        {
            _prefabHandles.Remove(releaseTypes[i]);
        }
    }

    public void TrySpawnRandomCoin(Vector3 spawnPos, float spawnChance)
    {
        if (!IsEnabled || _availableTypes.Count == 0 || UnityEngine.Random.value > spawnChance)
        {
            return;
        }

        SpecialCoinType selectedType = _availableTypes[UnityEngine.Random.Range(0, _availableTypes.Count)];
        SpawnCoin(selectedType, spawnPos);
    }

    public IEnumerator PreparePrefabsAsync(Action<float> onProgress = null)
    {
        if (!IsEnabled || _availableTypes.Count == 0)
        {
            onProgress?.Invoke(1f);

            yield break;
        }

        while (_isPreparingPrefabs)
        {
            yield return null;
        }

        if (_prefabsPrepared)
        {
            onProgress?.Invoke(1f);

            yield break;
        }

        _isPreparingPrefabs = true;
        bool allPrefabsLoaded = true;
        int total = _availableTypes.Count;

        for (int i = 0; i < total; i++)
        {
            SpecialCoinType type = _availableTypes[i];

            if (TryGetLoadedPrefab(type, out GameObject loadedPrefab) && loadedPrefab.GetComponent<SpecialCoin>() != null)
            {
                onProgress?.Invoke((float)(i + 1) / total);

                continue;
            }

            _loadingTypes.Add(type);
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(_referenceLookup[type]);
            _prefabHandles[type] = handle;

            while (!handle.IsDone)
            {
                onProgress?.Invoke((i + handle.PercentComplete) / total);

                yield return null;
            }

            _loadingTypes.Remove(type);

            if (!IsEnabled)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                _prefabHandles.Remove(type);
                _isPreparingPrefabs = false;
                onProgress?.Invoke(1f);

                yield break;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.Result.GetComponent<SpecialCoin>() == null)
            {
                Debug.LogError($"특수 코인 Addressables 사전 로드 실패: {type}", this);

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                _prefabHandles.Remove(type);
                allPrefabsLoaded = false;
            }

            onProgress?.Invoke((float)(i + 1) / total);
        }

        _prefabsPrepared = allPrefabsLoaded;
        _isPreparingPrefabs = false;
        onProgress?.Invoke(1f);
    }

    public SpecialCoin SpawnCoin(SpecialCoinType type, Vector3 spawnPos)
    {
        if (!IsEnabled || !_referenceLookup.ContainsKey(type))
        {
            return null;
        }

        if (TryGetLoadedPrefab(type, out GameObject prefab))
        {
            return SpawnLoadedCoin(type, prefab, spawnPos);
        }

        if (!_pendingSpawnPositions.TryGetValue(type, out List<Vector3> positions))
        {
            positions = new List<Vector3>();
            _pendingSpawnPositions[type] = positions;
        }
        positions.Add(spawnPos);

        if (_loadingTypes.Add(type))
        {
            StartCoroutine(LoadPrefabAndSpawn(type));
        }

        return null;
    }

    private IEnumerator LoadPrefabAndSpawn(SpecialCoinType type)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(_referenceLookup[type]);
        _prefabHandles[type] = handle;

        while (!handle.IsDone)
        {
            yield return null;
        }

        _loadingTypes.Remove(type);

        if (!IsEnabled)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            _prefabHandles.Remove(type);
            _pendingSpawnPositions.Remove(type);

            yield break;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.Result.GetComponent<SpecialCoin>() == null)
        {
            Debug.LogError($"특수 코인 Addressables 로드 실패: {type}", this);

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            _prefabHandles.Remove(type);
            _pendingSpawnPositions.Remove(type);

            yield break;
        }

        if (_pendingSpawnPositions.TryGetValue(type, out List<Vector3> positions))
        {
            _pendingSpawnPositions.Remove(type);

            for (int i = 0; i < positions.Count; i++)
            {
                if (!IsEnabled)
                {
                    break;
                }

                SpawnLoadedCoin(type, handle.Result, positions[i]);
            }
        }
    }

    private bool TryGetLoadedPrefab(SpecialCoinType type, out GameObject prefab)
    {
        prefab = null;

        return _prefabHandles.TryGetValue(type, out AsyncOperationHandle<GameObject> handle) && handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded && (prefab = handle.Result) != null;
    }

    private SpecialCoin SpawnLoadedCoin(SpecialCoinType type, GameObject prefab, Vector3 spawnPos)
    {
        Queue<SpecialCoin> pool = _coinPools[type];
        SpecialCoin coin = null;

        while (pool.Count > 0 && coin == null)
        {
            coin = pool.Dequeue();
        }

        if (coin == null)
        {
            coin = Instantiate(prefab, transform).GetComponent<SpecialCoin>();
        }

        if (coin == null)
        {
            Debug.LogError($"로드한 특수 코인에 SpecialCoin 컴포넌트가 없습니다: {type}", this);

            return null;
        }

        coin.transform.position = spawnPos;
        coin.transform.rotation = prefab.transform.rotation;
        coin.gameObject.SetActive(true);
        coin.Init(spawnPos);
        _activeCoins.Add(coin);

        return coin;
    }

    public void ReturnCoin(SpecialCoin coin)
    {
        if (coin == null)
        {
            return;
        }

        _activeCoins.Remove(coin);
        coin.gameObject.SetActive(false);

        if (!IsEnabled)
        {
            Destroy(coin.gameObject);

            return;
        }

        coin.transform.SetParent(transform);

        if (_coinPools.TryGetValue(coin.CoinType, out Queue<SpecialCoin> pool))
        {
            pool.Enqueue(coin);
        }
        else
        {
            Destroy(coin.gameObject);
        }
    }
}
