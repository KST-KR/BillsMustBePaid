using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [AMENDS - 01] 랜덤 보상 로직
/// </summary>
public class RandomRewardManager : MonoBehaviour
{
    #region 인스펙터
    [Header("데이터")]
    [SerializeField] private PigBankGameDataSO _pigData;

    [Header("스폰위치")]
    [SerializeField] private Transform _spawnPoint;
    #endregion

    #region 내부변수
    private int _currentHP;
    #endregion

    #region 이벤트
    void Start()
    {
        if (_pigData == null)
        {
            Debug.LogWarning("PigData 확인 필요");
            return;
        }

        _currentHP = _pigData.PigBankHP;

        if (_spawnPoint == null)
        {
            _spawnPoint = transform;
        }
    }
    #endregion

    #region 커스텀 메소드 (Public)
    public void TakeDamage(int damage)
    {
        if (_currentHP <= 0)
        {
            return;
        }

        _currentHP -= damage;
        Debug.Log($"현재 체력: {_currentHP}");

        if (_currentHP <= 0)
        {
            Die();
        }
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void Die()
    {
        Debug.Log("돼지 저금통 파괴");

        DropCoins();

        //TODO : 돼지 저금통을 풀로 되돌려야 함
    }

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
            Debug.Log("★ 잭팟! 코인 대량 드랍!");
        }

        for(int i = 0; i < coinCount; i++)
        {
            //TODO : 설정된 코인 갯수만큼 사방으로 흩뿌리기, 코인 오브젝트 풀에서 코인 가져오기
        }
    }
    #endregion

}
