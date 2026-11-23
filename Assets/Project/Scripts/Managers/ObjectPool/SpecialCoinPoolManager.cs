using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SpecialCoinInfo
{
    [Tooltip("특수 코인 종류")]
    public SpecialCoinType _coinType;

    [Tooltip("특수 코인 프리팹")]
    public SpecialCoin _coinPrefab;
}

public class SpecialCoinPoolManager : Singleton<SpecialCoinPoolManager>
{
    #region 인스펙터
    [Header("특수 코인 데이터 설정")]
    [SerializeField] private SpecialCoinInfo[] _specialCoinTable;
    #endregion

    #region 내부 변수
    private readonly Dictionary<SpecialCoinType, Queue<SpecialCoin>> _coinPools = new Dictionary<SpecialCoinType, Queue<SpecialCoin>>();
    private readonly Dictionary<SpecialCoinType, SpecialCoin> _prefabLookup = new Dictionary<SpecialCoinType, SpecialCoin>();
    private readonly List<SpecialCoinType> _availableTypes = new List<SpecialCoinType>();
    private readonly HashSet<SpecialCoin> _activeCoins = new HashSet<SpecialCoin>();

    public bool IsEnabled => SettingsManager.Instance == null || SettingsManager.Instance.CoinEnabled;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        if(_specialCoinTable == null)
        {
            _specialCoinTable = Array.Empty<SpecialCoinInfo>();
        }

        InitPools();

        if(SettingsManager.Instance != null)
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
    }
    #endregion

    #region 커스텀 함수(private)
    private void InitPools()
    {
        _coinPools.Clear();
        _prefabLookup.Clear();
        _availableTypes.Clear();

        foreach (SpecialCoinType type in Enum.GetValues(typeof(SpecialCoinType)))
        {
            _coinPools[type] = new Queue<SpecialCoin>();
        }

        for (int i = 0; i < _specialCoinTable.Length; i++)
        {
            SpecialCoinInfo info = _specialCoinTable[i];
            if (info._coinPrefab == null)
            {
                continue;
            }

            _prefabLookup[info._coinType] = info._coinPrefab;
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
                coin.gameObject.SetActive(false);
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
    }
    #endregion

    #region 커스텀 함수(public)
    /// <summary>
    /// 확률 체크 후 등록된 프리팹 배열 중 랜덤하게 1개를 풀에서 꺼내 배치
    /// </summary>
    public void TrySpawnRandomCoin(Vector3 spawnPos, float spawnChance)
    {
        if (!IsEnabled || _availableTypes.Count == 0)
        {
            return;
        }

        if (UnityEngine.Random.value > spawnChance)
        {
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _availableTypes.Count);
        SpecialCoinType selectedType = _availableTypes[randomIndex];

        SpawnCoin(selectedType, spawnPos);
    }

    /// <summary>
    /// 풀에서 비활성화된 코인을 꺼내거나 없으면 새로 생성
    /// </summary>
    public SpecialCoin SpawnCoin(SpecialCoinType type, Vector3 spawnPos)
    {
        if (!IsEnabled || !_prefabLookup.TryGetValue(type, out SpecialCoin prefab) || prefab == null)
        {
            return null;
        }

        Queue<SpecialCoin> pool = _coinPools[type];
        SpecialCoin coin = null;

        while (pool.Count > 0 && coin == null)
        {
            coin = pool.Dequeue();
        }

        if (coin == null)
        {
            coin = Instantiate(prefab, transform);
        }

        coin.transform.position = spawnPos;
        coin.transform.rotation = prefab.transform.rotation;
        coin.gameObject.SetActive(true);
        coin.Init(spawnPos);

        _activeCoins.Add(coin);
        return coin;
    }

    /// <summary>
    /// 사용이 끝난 코인을 풀로 반환
    /// </summary>
    public void ReturnCoin(SpecialCoin coin)
    {
        if (coin == null)
        {
            return;
        }

        _activeCoins.Remove(coin);

        if (!IsEnabled)
        {
            coin.gameObject.SetActive(false);
            Destroy(coin.gameObject);
            return;
        }

        coin.gameObject.SetActive(false);
        coin.transform.SetParent(transform);

        if (_coinPools.TryGetValue(coin.CoinType, out Queue<SpecialCoin> pool))
        {
            pool.Enqueue(coin);
        }
    }
    #endregion
}
