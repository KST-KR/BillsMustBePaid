using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 청구 정보 데이터를 저장
/// 요셉: 청구서 UI에 표시 및 금액 계산에 사용
/// </summary>
public class BillData : ScriptableObject
{
    #region 속성
    public int BaseAmount => _baseAmount;
    public int IncreasePerCycle => _increasePerCycle;
    #endregion

    #region 인스펙터
    [SerializeField, Min(1)] private int _baseAmount = 100;
    [SerializeField, Min(0)] private int _increasePerCycle = 50; 
    #endregion
}
