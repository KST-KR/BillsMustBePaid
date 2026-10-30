/// <summary>
/// 게임 씬에 전달할 실행 정보
/// </summary>
public static class GameLaunchData
{
    #region 열거형
    public enum ELaunchMode
    {
        NewGame,
        LoadGame,
        OpenSkillTree
    }
    #endregion

    #region 프로퍼티
    public static ELaunchMode LaunchMode { get; private set; }
    public static int SlotIndex { get; private set; } = -1;
    #endregion
    
    public static void SetLaunchData(ELaunchMode mode, int slotIndex)
    {
        LaunchMode = mode;
        SlotIndex = slotIndex;
    }

    public static void Clear()
    {
        LaunchMode = ELaunchMode.NewGame;
        SlotIndex = -1;
    }
}