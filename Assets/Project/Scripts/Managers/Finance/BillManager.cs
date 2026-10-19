using System;
using UnityEngine;

/// <summary>
/// 청구서 발행과 납부 가능 여부 및 결과를 관리
/// 요셉: 구독 관련 UI와 런 흐름을 갱신
/// </summary>
public class BillManager : MonoBehaviour
{
    #region 싱글톤
    public static BillManager Instance { get; private set; }
    #endregion

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
    /// 새 청구서를 발행하고 UI 에 알림
    /// 요셉: 청구서 이름과 금액을 표시할 때 OnBillIssued를 사용
    /// </summary>
    public bool IssueBill(BillData billData, int cycle)
    {
        return true;
    }

    /// <summary>
    /// 보유 재화가 충분할 때 청구서를 납부하고, 부족하면 실패 이벤트를 발생
    /// 요셉: 납부 입력에서 호출하고 결과 이벤트로 성공/실패를 처리
    /// </summary>
    public bool TryPayCurrentBill()
    {

        return true;
    }

    /// <summary>
    /// 미납 청구서를 취소하고 청구 상태를 비우기
    /// </summary>
    public void CancelCurrentBill()
    {
    } 
    #endregion
}

