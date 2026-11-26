using UnityEngine;

/// <summary>
/// 진열된 망치와 EquipmentData 연결
/// </summary>
public class ShopItem : MonoBehaviour
{
    #region 인스펙터
    [Header("장비 데이터")]
    [SerializeField] private EquipmentData _equipmentData;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 진열품에 연결된 장비 데이터
    /// </summary>
    public EquipmentData EquipmentData => _equipmentData;
    #endregion
}
