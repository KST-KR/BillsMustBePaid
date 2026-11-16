using System;

/// <summary>
/// 돼지 저금통 파괴, 해금 레벨 저장 데이터 
/// </summary>
[Serializable]
public class RunPiggyBankSaveData 
{
    // 해금 레벨
    public int unlockLevel = 1;
    public int unlockTotalCrash = 0;

    // 파괴 레벨
    public int crashLevel = 1;
    public int crashTotalNumber = 0;
}
