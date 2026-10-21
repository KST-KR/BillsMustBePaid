using System;

/// <summary>
/// 런이 끝나도 유지되는 플레이어의 재화
/// 영구적인 성장 스탯 
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public int legacyPoints;
    public PermanentGrowthSaveData permanentGrowth;

    public PlayerSaveData()
    {
        permanentGrowth = new PermanentGrowthSaveData();
    }
}