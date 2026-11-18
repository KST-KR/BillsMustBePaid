using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CoinInfo
{
    [Tooltip("코인 프리팹")]
    public GameObject _coinPrefab;

    [Tooltip("코인 값 설정")]
    public int _value;
}

public class CoinObjectPoolManager : Singleton<CoinObjectPoolManager>
{
    #region Enum
    public enum CoinType
    {
        GOLD = 0,
        SILVER = 1
    }
    #endregion

    #region 인스펙터
    [Header("해금 데이터 설정 (인스펙터에서 수정 가능)")]
    [SerializeField] private CoinInfo[] _coinTable;
    #endregion

    #region 내부 변수
    private Dictionary<CoinType, Queue<GameObject>> _coinPools = new Dictionary<CoinType, Queue<GameObject>>();
    private readonly HashSet<GameObject> _activeCoins = new HashSet<GameObject>();

    public bool IsEnabled => SettingsManager.Instance == null || SettingsManager.Instance.CoinEnabled;

    #endregion

    #region 이벤트
    protected override void OnSingletonAwake()
    {
        if (_coinTable == null)
        {
            _coinTable = new CoinInfo[0];
        }

        InitializePool();
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
    }
    #endregion

    #region 커스텀 메소드 (Public)
    /// <summary>
    /// 특정 타입의 코인을 풀에서 가져옴
    /// </summary>
    public GameObject GetCoin(CoinType type, Vector3 position, Quaternion rotation)
    {
        if (!IsEnabled)
        {
            return null;
        }

        int index = (int)type;
        if (_coinTable == null || index < 0 || index >= _coinTable.Length || _coinTable[index]._coinPrefab == null)
        {
            return null;
        }

        if(!_coinPools.ContainsKey(type))
        {
            _coinPools[type] = new Queue<GameObject>();
        }

        Queue<GameObject> pool = _coinPools[type];
        GameObject coin;

        if(pool.Count > 0)
        {
            coin = pool.Dequeue();
        }

        else
        {
            GameObject prefab = _coinTable[index]._coinPrefab;
            coin = Instantiate(prefab, transform);
        }

        coin.transform.position = position;
        coin.transform.rotation = rotation;
        coin.SetActive(true);
        _activeCoins.Add(coin);
        return coin;      
    }

    /// <summary>
    /// 사용이 끝난 코인을 반환
    /// </summary>
    public void ReturnCoin(CoinType type, GameObject coin)
    {
        if (coin == null)
        {
            return;
        }

        _activeCoins.Remove(coin);
        if (!IsEnabled)
        {
            coin.SetActive(false);
            Destroy(coin);
            return;
        }

        coin.SetActive(false);
        coin.transform.SetParent(transform);

        if(_coinPools.ContainsKey(type))
        {
            _coinPools[type].Enqueue(coin);
        }
    }

    /// <summary>
    /// 코인 타입에 해당하는 설정 정보(가치 등)를 가져옵니다.
    /// </summary>
    public CoinInfo GetCoinInfo(CoinType type)
    {
        int index = (int)type;
        if (index >= 0 && index < _coinTable.Length)
        {
            return _coinTable[index];
        }
        return default;
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void InitializePool()
    {
        foreach (CoinType type in Enum.GetValues(typeof(CoinType)))
        {
            _coinPools[type] = new Queue<GameObject>();
        }
    }

    private void HandleSettingsChanged()
    {
        if (IsEnabled)
        {
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
                {
                    Destroy(coin);
                }
            }
            pool.Clear();
        }
    }
    #endregion
}
