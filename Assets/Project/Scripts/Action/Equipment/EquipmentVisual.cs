using UnityEngine;

/// <summary>
/// 플레이어에게 장착되는 망치 모델의 장비 정보를 관리
/// </summary>
public class EquipmentVisual : MonoBehaviour
{
    #region 인스펙터
    [Header("장비 데이터")]
    [SerializeField] private EquipmentData _equipmentData;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 모델에 연결된 장비 데이터
    /// </summary>
    public EquipmentData EquipmentData => _equipmentData;
    #endregion
}
