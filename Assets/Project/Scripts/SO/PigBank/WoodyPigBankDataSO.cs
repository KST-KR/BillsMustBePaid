using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "3D/Data/Woody PigBank Data", fileName = "PigBankDataSO_Woody")]
public class WoodyPigBankDataSO : PigBankGameDataSO
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
