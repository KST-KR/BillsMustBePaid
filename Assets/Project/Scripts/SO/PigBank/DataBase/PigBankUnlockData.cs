using UnityEngine;

/// <summary>
/// 돼지 저금통 해금 규칙을 관리하는 정적 데이터
/// </summary>
[CreateAssetMenu(fileName = "PigBankUnlockData", menuName = "Bills Must Be Paid/Pig Bank Unlock Data")]
public sealed class PigBankUnlockData : ScriptableObject
{
    [SerializeField] private UnlockInfo[] _unlockTable;

    public UnlockInfo[] UnlockTable => _unlockTable;
}