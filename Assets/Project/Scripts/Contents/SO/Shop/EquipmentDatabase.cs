using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 판매할 망치 데이터 목록 관리
/// </summary>
[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Shop/Equipment Database")]
public class EquipmentDatabase : ScriptableObject
{
	#region 인스펙터
	[Header("판매 장비 목록")]
	[SerializeField] private List<EquipmentData> _equipments = new List<EquipmentData>();
	#endregion

	#region 프로퍼티
	/// <summary>
	/// 상점에서 판매하는 전체 장비 목록
	/// </summary>
	public IReadOnlyList<EquipmentData> Equipments => _equipments;
	#endregion

	#region 커스텀 (public)
	/// <summary>
	/// 장비 ID를 이용해 해당 장비 데이터를 찾기
	/// </summary>
	/// <param name="equipmnetId"></param>
	/// <returns></returns>
	public EquipmentData FindEquipmentById(string equipmentId)
	{
		if (string.IsNullOrWhiteSpace(equipmentId))
		{
			return null;
		}

        for (int i = 0; i < _equipments.Count; i++)
        {
            EquipmentData equipment = _equipments[i];

            if (equipment == null)
            {
                continue;
            }

            if (equipment.EquipmentId == equipmentId)
            {
                return equipment;
            }
        }

        return null;
	}
	#endregion
}
