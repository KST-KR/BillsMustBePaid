using System;

/// <summary>
/// 현재 진행 중인 런의 데이터를 저장
/// </summary>
[Serializable]
public class RunSaveData
{
    public bool isRunActive;
    public int currentStage;

    public int currentGold;
    public float currentStamina;

    public RunGrowthSaveData runGrowth;

    public RunSaveData()
    {
        runGrowth = new RunGrowthSaveData();
    }
}