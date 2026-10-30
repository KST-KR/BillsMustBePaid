using UnityEngine;

public class PigBankHealth : MonoBehaviour, IPlayerAttackTarget
{
    private PigBankGameDataSO _gameData;
    private int _currentHP;

    public void Initialize(PigBankGameDataSO gameData)
    {
        if (gameData == null)
        {
            Debug.LogError("돼지 저금통 데이터가 없음.", this);
            return;
        }

        _gameData = gameData;
        ResetHealth();
    }

    public void ResetHealth()
    {
       
        _currentHP = Mathf.Max(0, _gameData.PigBankHP);
        
    }

    public void TakePlayerAttack(float damage)
    {
        Debug.Log(
        $"[피격 함수 진입] damage={damage}, " +
        $"gameDataNull={_gameData == null}, currentHP={_currentHP}",
        this);

        if (_gameData == null || _currentHP <= 0)
        {
            Debug.LogWarning(
            "[피격 처리 중단] 데이터가 없거나 HP가 0 이하",
            this);
            return;
        }

        int damageAmount = Mathf.Max(0, Mathf.RoundToInt(damage));
        if (damageAmount <= 0)
        {
            Debug.LogWarning("[피격 처리 중단] 피해량이 0 이하임.", this);
            return;
        }

        _currentHP = Mathf.Max(0, _currentHP - damageAmount);

        // 피격시 색 변화
        HitEffect hitEffect = GetComponent<HitEffect>();
        if (hitEffect != null)
        {
            hitEffect.ChangeColor();
        }
        else
        {
            Debug.LogError("돼지 저금통에 HitEffect가 없습니다!", this);
        }

        Debug.Log($"{_gameData.PigBankName} 피격: 피해량 {damageAmount}, 남은 HP {_currentHP}");

        if (_currentHP <= 0)
        {
            // 파괴 및 보상 처리는 돼지 저금통 담당 로직과 연결
            RandomRewardManager rewardManager = GetComponent<RandomRewardManager>();
            if (rewardManager != null)
            {
                rewardManager.Die();
            }
            else
            {
                Debug.LogError("돼지 저금통에 RandomRewardManager가 없습니다!", this);
            }
        }
    }
}