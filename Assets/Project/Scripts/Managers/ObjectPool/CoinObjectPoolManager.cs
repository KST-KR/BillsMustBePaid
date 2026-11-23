using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public struct CoinInfo
{
    [Tooltip("Addressable 코인 프리팹")]
    public AssetReferenceGameObject _coinPrefabReference;

    [Tooltip("Addressable 참조가 할당되지 않은 경우 레거시 대체(fallback)가 사용")]
    public GameObject _coinPrefab;

    [Tooltip("Coin 값")]
    public int _value;
}

public class CoinObjectPoolManager : Singleton<CoinObjectPoolManager>
{
    public enum CoinType
    {
        GOLD = 0,
        SILVER = 1
    }

    [Header("코인 프리팹 테이블")]
    [SerializeField] private CoinInfo[] _coinTable;

    private readonly Dictionary<CoinType, Queue<GameObject>> _coinPools = new Dictionary<CoinType, Queue<GameObject>>();
    private readonly Dictionary<CoinType, GameObject> _prefabLookup = new Dictionary<CoinType, GameObject>();
    private readonly List<AsyncOperationHandle<GameObject>> _loadedHandles = new List<AsyncOperationHandle<GameObject>>();
    private readonly HashSet<GameObject> _activeCoins = new HashSet<GameObject>();
    private bool _isPreparingPrefabs;
    private bool _prefabsReady;

    public bool IsEnabled => SettingsManager.Instance == null || SettingsManager.Instance.CoinEnabled;

    protected override void OnSingletonAwake()
    {
        if (_coinTable == null)
            _coinTable = Array.Empty<CoinInfo>();

        InitializePool();
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
    }

    protected override void OnSingletonDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;

        for (int i = 0; i < _loadedHandles.Count; i++)
        {
            if (_loadedHandles[i].IsValid())
                Addressables.Release(_loadedHandles[i]);
        }
        _loadedHandles.Clear();
    }

    public IEnumerator PreparePrefabsAsync(Action<float> onProgress = null)
    {
        if (!IsEnabled)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        while (_isPreparingPrefabs)
            yield return null;

        if (_prefabsReady)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        _isPreparingPrefabs = true;
        int total = _coinTable != null ? _coinTable.Length : 0;
        for (int i = 0; i < total; i++)
        {
            CoinType type = (CoinType)i;
            CoinInfo info = _coinTable[i];
            GameObject prefab = null;
            AssetReferenceGameObject reference = info._coinPrefabReference;

            if (reference != null && reference.RuntimeKeyIsValid())
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(reference);
                while (!handle.IsDone)
                {
                    float typeProgress = handle.PercentComplete * 0.8f;
                    onProgress?.Invoke(total == 0 ? 1f : (i + typeProgress) / total);
                    yield return null;
                }

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    prefab = handle.Result;
                    _loadedHandles.Add(handle);
                }
                else
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    Debug.LogError($"Addressable 코인 프리팹을 불러오는 데 실패: {type}", this);
                }
            }

            if (prefab == null)
                prefab = info._coinPrefab;

            if (prefab != null)
                _prefabLookup[type] = prefab;
            else
                Debug.LogError($"해당 유형에 할당된 코인 프리팹이 없음 {type}.", this);

            onProgress?.Invoke(total == 0 ? 1f : (float)(i + 1) / total);
        }

        _prefabsReady = true;
        _isPreparingPrefabs = false;
    }

    public GameObject GetCoin(CoinType type, Vector3 position, Quaternion rotation)
    {
        if (!IsEnabled)
            return null;

        if (!_prefabsReady || !_prefabLookup.TryGetValue(type, out GameObject prefab))
            return null;

        if (!_coinPools.TryGetValue(type, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _coinPools[type] = pool;
        }

        GameObject coin = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        coin.transform.SetPositionAndRotation(position, rotation);
        coin.SetActive(true);

        if (coin.TryGetComponent(out CoinBounce bounce))
            bounce.Init(position);

        _activeCoins.Add(coin);
        return coin;
    }

    public void ReturnCoin(CoinType type, GameObject coin)
    {
        if (coin == null)
            return;

        _activeCoins.Remove(coin);
        if (!IsEnabled)
        {
            coin.SetActive(false);
            Destroy(coin);
            return;
        }

        coin.SetActive(false);
        coin.transform.SetParent(transform);
        if (_coinPools.TryGetValue(type, out Queue<GameObject> pool))
            pool.Enqueue(coin);
    }

    public CoinInfo GetCoinInfo(CoinType type)
    {
        int index = (int)type;
        if (_coinTable != null && index >= 0 && index < _coinTable.Length)
            return _coinTable[index];
        return default;
    }

    private void InitializePool()
    {
        _coinPools.Clear();
        foreach (CoinType type in Enum.GetValues(typeof(CoinType)))
            _coinPools[type] = new Queue<GameObject>();
    }

    private void HandleSettingsChanged()
    {
        if (IsEnabled)
        {
            if (!_prefabsReady && !_isPreparingPrefabs)
                StartCoroutine(PreparePrefabsAsync());
            return;
        }

        foreach (GameObject coin in _activeCoins)
        {
            if (coin != null)
            {
                coin.SetActive(false);
                Destroy(coin);
            }
        }
        _activeCoins.Clear();

        foreach (Queue<GameObject> pool in _coinPools.Values)
        {
            foreach (GameObject coin in pool)
            {
                if (coin != null)
                    Destroy(coin);
            }
            pool.Clear();
        }
    }
}
