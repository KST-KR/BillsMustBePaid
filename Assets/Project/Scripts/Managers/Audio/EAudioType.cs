/// <summary>
/// 게임에서 사용하는 사운드 종류를 정의합니다.
/// </summary>
public enum EAudioType
{
    // BGM
    TitleBGM,
    GameBGM,

    // 플레이어 및 전투
    PlayerAttack,
    PigHit,
    PigDestroy,

    // 재화 및 성장
    CoinGet,
    Upgrade,

    // 게임 진행
    StageClear,
    GameOver,

    // UI
    ButtonClick
}

