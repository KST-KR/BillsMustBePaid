using System;

/// <summary>
/// 저장 슬롯 하나의 전체 데이터를 담는 클래스
/// </summary>
[Serializable]
public class SaveSlotData
{
    // 저장 슬롯 구분
    public int slotIndex;
    // 가장 최근에 플레이한 슬롯을 판별
    public string lastPlayedAt;

    // 런이 종료되어도 유지되는 데이터를 보관
    public PlayerSaveData playerData;
    // 게임을 종료했다가 다시 실행했을 때 이어할 진행 데이터 보관
    public RunSaveData runData;

    public SaveSlotData()
    {
        playerData = new PlayerSaveData();
        runData = new RunSaveData();
    }
}