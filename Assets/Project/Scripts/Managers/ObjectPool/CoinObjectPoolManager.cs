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

public class CoinObjectPoolManager : MonoBehaviour
{
    #region Enum
    public enum CoinType
    {
        GOLD = 0,
        SILVER = 1
    }
    #endregion

    #region 싱글톤
    public static CoinObjectPoolManager Instance { get; private set; }
    #endregion

    #region 인스펙터
    [Header("해금 데이터 설정 (인스펙터에서 수정 가능)")]
    [SerializeField] private CoinInfo[] _coinTable;

    [Header("생성 개수")]
    [SerializeField] private int _generationCount = 50;
    #endregion

    #region 내부 변수
    private Dictionary<CoinType, Queue<GameObject>> _coinPools = new Dictionary<CoinType, Queue<GameObject>>();

    #endregion

    #region 이벤트
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);

            return;
        }

        InitializePool();
    }
    #endregion

    #region 커스텀 메소드 (Public)
    /// <summary>
    /// 특정 타입의 코인을 풀에서 가져옴
    /// </summary>
    public GameObject GetCoin(CoinType type, Vector3 position, Quaternion rotation)
    {
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
            int index = (int)type;
            GameObject prefab = _coinTable[index]._coinPrefab;
            coin = Instantiate(prefab, transform);
        }

        coin.transform.position = position;
        coin.transform.rotation = rotation;
        coin.SetActive(true);
        return coin;      
    }

    /// <summary>
    /// 사용이 끝난 코인을 반환
    /// </summary>
    public void ReturnCoin(CoinType type, GameObject coin)
    {
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
        // Enum에 정의된 모든 코인 종류를 순회하며 각각 풀 생성
        foreach (CoinType type in Enum.GetValues(typeof(CoinType)))
        {
            _coinPools[type] = new Queue<GameObject>();

            int index = (int)type;
            if (index >= _coinTable.Length) continue;

            GameObject prefab = _coinTable[index]._coinPrefab;
            if (prefab == null) continue;

            for (int i = 0; i < _generationCount; i++)
            {
                GameObject coin = Instantiate(prefab, transform);
                coin.SetActive(false);
                _coinPools[type].Enqueue(coin);
            }
        }
    }
    #endregion
}
