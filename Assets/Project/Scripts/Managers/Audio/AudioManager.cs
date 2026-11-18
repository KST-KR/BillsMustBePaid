using System;
using UnityEngine;

/// <summary>
/// 게임의 BGM 및 효과음 재생을 관리합니다.
/// 사전 매핑된 사운드를 외부 요청에 따라 재생합니다.
/// </summary>
public class AudioManager : PersistentSingleton<AudioManager>
{
    #region 속성
    public float BGMVolume => _bgmVolume;
    public float SFXVolume => _sfxVolume;
    #endregion

    #region 이벤트

    public event Action<EAudioType> OnAudioRequested;

    #endregion

    #region 인스펙터 변수

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Audio Mapping")]
    [SerializeField] private AudioData[] _audioData;

    [Header("Audio Volume")]
    [SerializeField, Range(0f, 1f)]
    private float _masterVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float _bgmVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float _sfxVolume = 1f;

    #endregion

    protected override void OnSingletonAwake()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
        ApplySavedVolumes();
    }

    protected override void OnSingletonDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }
    }

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 지정한 BGM을 재생합니다.
    /// </summary>
    public void PlayBGM(EAudioType audioType)
    {

        AudioClip clip = FindAudioClip(audioType);

        if(clip == null)
        {
            return;
        }

        _bgmSource.clip = clip;
        ApplyBGMVolume();
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    /// <summary>
    /// 현재 BGM을 정지합니다.
    /// </summary>
    public void StopBGM()
    {

        _bgmSource.Stop();
    }

    /// <summary>
    /// 지정한 효과음을 재생합니다.
    /// </summary>
    public void PlaySFX(EAudioType audioType)
    {

        AudioClip clip = FindAudioClip(audioType);

        if( clip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// BGM을 일시정지합니다.
    /// </summary>
    public void PauseBGM()
    {

        _bgmSource.Pause();
    }

    /// <summary>
    /// 일시정지된 BGM을 다시 재생합니다.
    /// </summary>
    public void ResumeBGM()
    {

        _bgmSource.UnPause();
    }

    /// <summary>
    /// BGM 볼륨을 변경합니다.
    /// </summary>
    public void SetBGMVolume(float volume)
    {

        _bgmVolume = Mathf.Clamp01(volume);
        ApplyBGMVolume();
    }

    /// <summary>
    /// 효과음 볼륨을 변경합니다.
    /// </summary>
    public void SetSFXVolume(float volume)
    {

        _sfxVolume = Mathf.Clamp01(volume);
        ApplySFXVolume();
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        ApplyBGMVolume();
        ApplySFXVolume();
    }

    /// <summary>
    /// 지정한 사운드의 재생을 요청합니다.
    /// </summary>
    public void RequestAudio(EAudioType audioType)
    {
        OnAudioRequested?.Invoke(audioType);
    }

    #endregion

    #region 커스텀 메서드 (Private)

    private void ApplySavedVolumes()
    {
        SettingsManager settingsManager = SettingsManager.Instance;
        if (settingsManager != null)
        {
            SaveSettingData settings = settingsManager.Settings;
            _masterVolume = Mathf.Clamp01(settings.masterVolume);
            _bgmVolume = Mathf.Clamp01(settings.musicVolume);
            _sfxVolume = Mathf.Clamp01(settings.sfxVolume);
        }

        ApplyBGMVolume();
        ApplySFXVolume();
    }

    private void HandleSettingsChanged()
    {
        ApplySavedVolumes();
    }

    private void ApplyBGMVolume()
    {
        if (_bgmSource != null)
        {
            _bgmSource.volume = _bgmVolume * _masterVolume;
        }
    }

    private void ApplySFXVolume()
    {
        if (_sfxSource != null)
        {
            _sfxSource.volume = _sfxVolume * _masterVolume;
        }
    }

    /// <summary>
    /// 매핑된 사운드 클립을 검색합니다.
    /// </summary>
    private AudioClip FindAudioClip(EAudioType audioType)
    {

        foreach (var data in _audioData)
        {
            if(data.AudioType == audioType)
            {
                return data.AudioClip;
            }
        }

        Debug.LogError($"AudioType을 찾을 수 없습니다 : {audioType}");
        return null;
    }

    #endregion
}

