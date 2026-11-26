using UnityEngine;

[CreateAssetMenu(fileName = "PigCrashLevelData", menuName = "Bills Must Be Paid/Pig Crash Level Data")]
public sealed class PigCrashLevelData : ScriptableObject
{
    [Header("파괴 레벨 설정")]
    [SerializeField] private int _maxLevel = 5;
    [SerializeField] private CrashLevelInfo[] _levelTable;

    public int MaxLevel => _maxLevel;
    public CrashLevelInfo[] LevelTable => _levelTable;
}