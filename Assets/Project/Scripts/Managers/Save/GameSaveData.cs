using System;

/// <summary>
/// 게임 저장에 필요한 데이터를 보관합니다.
/// </summary>
[Serializable]
public class GameSaveData
{
    // 런 종료 후에도 유지되는 데이터
    public int LegacyPoint;

    // TODO: 팀원들과 협의 후 영구 저장 데이터 추가
}

