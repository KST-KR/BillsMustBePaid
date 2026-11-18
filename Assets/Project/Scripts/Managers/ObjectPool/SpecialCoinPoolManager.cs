using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialCoinPoolManager : Singleton<SpecialCoinPoolManager>
{
    #region 인스펙터
    [Header("특수 코인 프리팹")]
    [SerializeField] private SpecialCoin[] _specialCoinPrefabs;
    #endregion

    #region 내부 변수
    private List<SpecialCoin>[] _coinPools;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        InitPools();
    }
    #endregion

    #region 커스텀 함수(private)
    private void InitPools()
    {
        if (_specialCoinPrefabs == null)
        {
            return;
        }

        _coinPools = new List<SpecialCoin>[_specialCoinPrefabs.Length];

        for (int i = 0; i < _specialCoinPrefabs.Length; i++)
        {
            _coinPools[i] = new List<SpecialCoin>();
        }
    }
    #endregion

    #region 커스텀 함수(public)
    /// <summary>
    /// 확률 체크 후 등록된 프리팹 배열 중 랜덤하게 1개를 풀에서 꺼내 배치
    /// </summary>
    public void TrySpawnRandomCoin(Vector3 spawnPos, float spawnChance)
    {
        if (_specialCoinPrefabs == null || _specialCoinPrefabs.Length == 0)
        {
            return;
        }

        if (Random.value > spawnChance)
        {
            return;
        }

        int randomIndex = Random.Range(0, _specialCoinPrefabs.Length);
        SpecialCoin coin = GetCoin(randomIndex);

        if (coin != null)
        {
            coin.transform.position = spawnPos;
            coin.transform.rotation = _specialCoinPrefabs[randomIndex].transform.rotation;
            coin.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 풀에서 비활성화된 코인을 꺼내거나 없으면 새로 생성
    /// </summary>
    public SpecialCoin GetCoin(int index)
    {
        if (index < 0 || index >= _coinPools.Length)
        {
            return null;
        }

        foreach (SpecialCoin coin in _coinPools[index])
        {
            if (!coin.gameObject.activeInHierarchy)
            {
                return coin;
            }
        }

        SpecialCoin prefab = _specialCoinPrefabs[index];
        if (prefab == null)
        {
            return null;
        }

        SpecialCoin newCoin = Instantiate(prefab, transform);
        _coinPools[index].Add(newCoin);
        return newCoin;
    }

    /// <summary>
    /// 사용이 끝난 코인을 풀로 반환
    /// </summary>
    public void ReturnCoin(SpecialCoin coin)
    {
        if (coin != null)
        {
            coin.gameObject.SetActive(false);
        }
    }
    #endregion
}
