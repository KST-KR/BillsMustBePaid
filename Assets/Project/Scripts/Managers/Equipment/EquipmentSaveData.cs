using System;
using System.Collections.Generic;

/// <summary>
/// 런에서 사용하는 장비의 저장 데이터
/// </summary>
[Serializable]
public class EquipmentSaveData
{
    // 현재 장착 중인 장비 ID
    public string equippedEquipmentId;

    // 보유 중인 장비 ID 목록
    public List<string> ownedEquipmentIds = new List<string>();

    // 골드를 지불하고 구매한 장비 ID 목록
    public List<string> purchasedEquipmentIds = new List<string>();
}
