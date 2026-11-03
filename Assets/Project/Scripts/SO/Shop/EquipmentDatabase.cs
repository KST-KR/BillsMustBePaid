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
}
