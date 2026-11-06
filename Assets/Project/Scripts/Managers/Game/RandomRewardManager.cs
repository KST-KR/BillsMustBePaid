using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [AMENDS - 01] 랜덤 보상 로직
/// </summary>
public class RandomRewardManager : MonoBehaviour
{
    #region 내부변수
    private PigBankGameDataSO _pigData;
    private bool _isDead;
    #endregion

    #region 이벤트
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
        _isDead = false;

        if (_pigData == null)
        {
            Debug.LogWarning("PigData가 null입니다!");
            return;
        }
    }

    public void Die()
    {
        if(_isDead)
        {
            return;
        }

        _isDead = true;

        Debug.Log("돼지 저금통 파괴");

        DropCoins();

        if (StageRewardManager.Instance != null)
        {
            StageRewardManager.Instance.NotifyPigBankDestroyed();
        }

        PigBankUnlock.Instance.UnlockPigBank();



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

        // StageRewardManager를 통해 현재 레벨의 보너스 보상을 안전하게 가져옴
        int levelBonus = (StageRewardManager.Instance != null) ? StageRewardManager.Instance.GetCurrentLevelBonusReward() : 0;
        int coinCount = _pigData.RewardCoin + levelBonus;

        if (isJacpot)
        {
            coinCount *= 3;
            Debug.Log($"★ 잭팟! 코인 대량 드랍! (3배)(총 {coinCount}개)");
        }

        else
        {
            Debug.Log($"[Reward] 기본 코인 보상 계산: {coinCount}개");
        }

        if (CoinObjectPoolManager.Instance != null)
        {
            Vector3 spawnPosition = transform.position;

            // 코인 테이블 정보 가져오기 
            int goldValue = CoinObjectPoolManager.Instance.GetCoinInfo(CoinObjectPoolManager.CoinType.GOLD)._value;
            int silverValue = CoinObjectPoolManager.Instance.GetCoinInfo(CoinObjectPoolManager.CoinType.SILVER)._value;

            // 가치 높은 순서대로 개수 계산
            int goldCount = 0;
            int silverCount = 0;

            int remainingValue = coinCount;

            // 골드(100원) 단위로 나누기
            if (goldValue > 0)
            {
                goldCount = remainingValue / goldValue;
                remainingValue %= goldValue;
            }

            // 나머지 금액을 실버(10원) 단위로 나누기
            if (silverValue > 0)
            {
                silverCount = remainingValue / silverValue;
                remainingValue %= silverValue;
            }

            // 남은 잔돈(나누어 떨어지지 않는 값)이 있다면 실버 코인 1개 추가 처리
            if (remainingValue > 0)
            {
                silverCount += 1;
            }

            Debug.Log($"Coin Spawn : 총 {coinCount}원 -> 골드 {goldCount}, 실버 {silverCount} 생성");

            // 2. 계산된 골드 코인 생성 및 흩뿌리기
            SpawnCoinGroup(CoinObjectPoolManager.CoinType.GOLD, goldCount, spawnPosition);

            // 3. 계산된 실버 코인 생성 및 흩뿌리기
            SpawnCoinGroup(CoinObjectPoolManager.CoinType.SILVER, silverCount, spawnPosition);
        }

        if (StageRewardManager.Instance != null)
        {
            // 현재 누적되는 골드 양 확인
            Debug.Log($"추가하려는 코인: {coinCount}, 현재까지 누적된 스테이지 골드: {StageRewardManager.Instance.StageRewardGold}");
            StageRewardManager.Instance.TryAddStageGold(coinCount);
        }
    }

    /// <summary>
    /// 코인 종류별 개수만큼 위치를 살짝 분산시켜서 풀에서 꺼냄
    /// </summary>
    private void SpawnCoinGroup(CoinObjectPoolManager.CoinType type, int count, Vector3 basePosition)
    {
        for (int i = 0; i < count; i++)
        {
            // 코인들이 한 점에 겹치지 않도록 미세한 랜덤 오프셋 적용
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            Vector3 coinSpawnPos = basePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);

            // 오브젝트 풀에서 코인 가져오기
            GameObject coin = CoinObjectPoolManager.Instance.GetCoin(type, coinSpawnPos, Quaternion.identity);
        }
    }
    #endregion

}
