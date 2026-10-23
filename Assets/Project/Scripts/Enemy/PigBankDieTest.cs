using UnityEngine;

/// <summary>
/// A 키를 눌러 돼지 저금통을 공격하닌 테스트 시스템
/// </summary>
public class PigBankDieTest : MonoBehaviour
{
    #region 이벤트
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            AttackPig();
        }
    }
    #endregion

    #region 커스텀 매소드 (private)
    private void AttackPig()
    {
        RandomRewardManager pig = FindAnyObjectByType<RandomRewardManager>();

        if (pig != null)
        {
            Debug.Log($"PigAttacker : 돼지 탐색 성공! 대상 이름: {pig.gameObject.name}");
            pig.TakeDamage(5);
        }
        else
        {
            Debug.LogWarning("PigAttacker : 씬에 RandomRewardManager를 가진 돼지 저금통이 없습니다!");
        }
    }
    #endregion

}
