using System;
using System.Collections.Generic;

/// <summary>
/// 영구적으로 구매한 성장 아이템 정보를 저장
/// </summary>
[Serializable]
public class PermanentGrowthSaveData
{
    public List<string> purchasedItemIds = new List<string>();
}