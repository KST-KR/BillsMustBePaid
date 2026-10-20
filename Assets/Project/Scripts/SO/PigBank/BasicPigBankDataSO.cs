using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기본 돼지 저금통
/// </summary>
[CreateAssetMenu(menuName = "3D/Data/Basic PigBank Data", fileName = "PigBankDataSO_Basic")]
public class BasicPigBankDataSO : PigBankGameDataSO
{
    #region 프로퍼티
    public GameObject PigPrefab => _pigPrefab;
    #endregion

    #region 인스펙터
    [Header("돼지 저금통 설정")]
    [SerializeField] private GameObject _pigPrefab;
    #endregion

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
