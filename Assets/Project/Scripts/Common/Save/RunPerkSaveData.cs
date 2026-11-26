using System;
using System.Collections.Generic;

/// <summary>
/// 런 중 획득한 퍼크 상태 저장함
/// 카테고리별 다음 단계 진행값 저장함
/// 이어하기 복원시 동일 선택 흐름 보장함
/// </summary>
[Serializable]
public class RunPerkSaveData
{
    public List<string> ownedPerkIds = new List<string>();
    public List<int> nextTierByStat = new List<int>();

    public RunPerkSaveData()
    {
        ownedPerkIds = new List<string>();
        nextTierByStat = new List<int>();
    }
}
