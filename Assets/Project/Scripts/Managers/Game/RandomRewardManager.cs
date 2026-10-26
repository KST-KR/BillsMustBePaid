using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [AMENDS - 01] 랜덤 보상 로직
/// </summary>
public class RandomRewardManager : MonoBehaviour
{
    #region 싱글톤
    public static RandomRewardManager Instance {  get; private set; }
    #endregion

    #region 내부변수
    private PigBankGameDataSO _pigData;
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
        }
    }

    void Start()
    {
        if (_pigData == null)
        {
            Debug.LogWarning("PigData 확인 필요");
            return;
        }

    }
    #endregion

    #region 커스텀 메소드 (Public)
    /// <summary>
    /// 스폰될 때 데이터 세팅
    /// </summary>
    public void Initialize(PigBankGameDataSO pigData)
    {
        _pigData = pigData;

        if (_pigData == null)
        {
            Debug.LogWarning("PigData가 null입니다!");
            return;
        }

    }

    public void Die()
    {
        Debug.Log("돼지 저금통 파괴");

        DropCoins();

        if (PigBankObjectPoolManager.Instance != null)
        {
            PigBankObjectPoolManager.Instance.ReturnPigBank(_pigData, gameObject);
        }

        if (FragmentObjectPoolManager.Instance != null)
        {
            FragmentObjectPoolManager.Instance.SpawnRandomFragments(transform.position);
        }

    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void DropCoins()
    {
        if(_pigData == null)
        {
            return;
        }

        bool isJacpot = Random.value <= _pigData.JackpotChance;

        int coinCount = _pigData.RewardCoin;

        if(isJacpot)
        {
            coinCount *= 3;
            Debug.Log($"★ 잭팟! 코인 대량 드랍! (3배)(총 {coinCount}개)");
        }

        else
        {
            Debug.Log($"[Reward] 기본 코인 보상 계산: {coinCount}개");
        }


        if(StageRewardManager.Instance != null)
        {
            StageRewardManager.Instance.TryAddStageGold(coinCount);
        }
        //TODO : 설정된 코인 갯수만큼 사방으로 흩뿌리기, 코인 오브젝트 풀에서 코인 가져오기
    }
    #endregion

}
