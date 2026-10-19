using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "3D/Data/Basic PigBank Data", fileName = "BasicPigBankDataSO_")]
public class CBasicPigBankDataSO : CPigBankGameDataSO
{
    #region 인스펙터
    [Header("기본 돼지 저금통 설정")]
    [SerializeField] private GameObject _pigPrefab;
    #endregion

    #region 프로퍼티
    public GameObject PigPrefab => _pigPrefab;
    #endregion

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
