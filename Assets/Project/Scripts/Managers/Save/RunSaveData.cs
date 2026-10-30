using System;

/// <summary>
/// 현재 진행 중인 런의 데이터를 저장
/// </summary>
[Serializable]
public class RunSaveData
{
    public bool isRunActive;
    public int currentStage;

    // 현재 스테이지 진행 여부
    public bool isStageInProgress;

    public int currentGold;
    public float currentStamina;

    public RunGrowthSaveData runGrowth;

    public RunSaveData()
    {
        runGrowth = new RunGrowthSaveData();
    }
}