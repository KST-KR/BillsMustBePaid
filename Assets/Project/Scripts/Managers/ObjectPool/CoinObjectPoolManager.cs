using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinObjectPoolManager : MonoBehaviour
{
    #region 싱글톤
    public static CoinObjectPoolManager Instance { get; private set; }
    #endregion

    #region 인스펙터
    [Header("코인 설정")]
    [SerializeField] private GameObject _coinPrefab;
    [SerializeField] private int _generationCount = 50;
    #endregion

    #region 내부 변수
    private Queue<GameObject> _coinPool = new Queue<GameObject>();
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
    public GameObject GetCoin(Vector3 position, Quaternion rotation)
    {
        GameObject coin;

        if(_coinPool.Count > 0)
        {
            coin = _coinPool.Dequeue();
        }

        else
        {
            coin = Instantiate(_coinPrefab, transform);
        }

        coin.transform.position = position;
        coin.transform.rotation = rotation;
        coin.SetActive(true);
        return coin;      
    }

    public void ReturnCoin(GameObject coin)
    {
        coin.SetActive(false);
        coin.transform.SetParent(transform);
        _coinPool.Enqueue(coin);

        ResetCoinObject(coin);
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void InitializePool()
    {
        for(int i = 0;  i < _coinPool.Count; i++)
        {
            GameObject coin = Instantiate(_coinPrefab, transform);
            coin.SetActive(false);
            _coinPool.Enqueue(coin);
        }
    }

    private void ResetCoinObject(GameObject coin)
    {
        Rigidbody rb = coin.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
    #endregion
}
