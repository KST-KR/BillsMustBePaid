using System;

/// <summary>
/// 하나의 저장 슬롯에 기록할 게임 데이터
/// </summary>
[Serializable]
public class SaveData
{
    public int schemaVersion = 1;
    public string lastSavedAt;

    // 각 시스템의 저장 데이터는 담당자와 협의 후 추가
}