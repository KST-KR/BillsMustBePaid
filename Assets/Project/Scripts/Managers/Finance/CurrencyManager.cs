using System;
using UnityEngine;

/// <summary>
/// 현재 재화를 관리하고 변경 결과를 알림
/// 민재: 저금통 파괴 보상액을 Earn으로 전달해 재화에 반영
/// 요셉: UI에 표시할 재화 값을 갱신
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    #region 싱글톤
    public static CurrencyManager Instance { get; private set; } 
    #endregion

    #region 속성
    public int CurrentAmount => _currentAmount; 
    #endregion

    #region 이벤트
    public event Action<int> OnAmountChanged; 
    #endregion

    private int _currentAmount;

    #region 이벤트 함수
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// 보상 재화를 더하고 변경 이벤트를 발생
    /// 민재: 저금통 파괴 보상 금액을 전달해 재화에 반영
    /// </summary>
    public void Earn(int amount)
    {

    }

    /// <summary>
    /// 재화가 충분할 때만 지정한 금액을 차감
    /// 요셉: 납부/구매 입력을 처리할 때 차감 성공 여부를 확인
    /// </summary>
    public bool TrySpend(int amount)
    {
        return true;
    }

    /// <summary>
    /// 파산 패널티 비율만큼 재화를 차감하고 실제 차감액을 반환
    /// 요셉: OnAmountChanged 이벤트로 파산 처리 시 적용할 재화 패널티를 확인
    /// </summary>
    public int ApplyPenalty(float penaltyRate)
    {
        return 0;
    } 
    #endregion
}
