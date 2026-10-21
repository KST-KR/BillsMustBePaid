using System;

/// <summary>
/// 현재 런에서 획득한 추가 성장 수치를 저장
/// </summary>
[Serializable]
public class RunGrowthSaveData
{
    public float damage;
    public float attackSpeed;
    public float attackRange;
    public float critical;
    public float luck;
    public float maxStamina;
}