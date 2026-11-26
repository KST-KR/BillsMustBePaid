using System;

/// <summary>
/// 사용자 설정값을 저장하는 데이터입니다
/// </summary>
[Serializable]
public sealed class SaveSettingData
{
    public float masterVolume = 0.8f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public bool fullscreen = true;
    public float cursorSpeed = 0.8f;
    public float cursorSize = 0.8f;
    public float cursorColor = 0.18f;
    public float cursorOpacity = 1.0f;
    public int damageTextMode;
    public int moneyTextMode;
    public bool coinEnabled = true;
    public int fragmentAmount = 5;
}