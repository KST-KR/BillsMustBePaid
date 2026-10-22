using System;

/// <summary>
/// 개별 저장 슬롯의 관리 정보
/// </summary>
[Serializable]
public class SaveSlotMetadata
{
    public int slotId;
    public bool hasSaveData;
    public string lastPlayedAt;
}