using UnityEngine;

/// <summary>
/// [PL-ASKILL-01] 액티브 스킬 시스템 구성요소를 초기화
/// </summary>
public static class ActiveSkillSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        GrowthManager growthManager = Object.FindAnyObjectByType<GrowthManager>();
        if (growthManager == null)
        {
            return;
        }

        EnsureManager();
    }

    private static void EnsureManager()
    {
        ActiveSkillManager existing = Object.FindAnyObjectByType<ActiveSkillManager>();
        if (existing != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("ActiveSkillManager");
        managerObject.AddComponent<ActiveSkillManager>();
    }

}
