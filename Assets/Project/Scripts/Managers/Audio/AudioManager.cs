using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioManager : PersistentSingleton<AudioManager>
{
    public float BGMVolume => _bgmVolume;
    public float SFXVolume => _sfxVolume;

    public event Action<EAudioType> OnAudioRequested;

    [Header("오디오 소스")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("오디오 매핑")]
    [SerializeField] private AudioData[] _audioData;

    [Header("오디오 볼륨")]
    [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

    private readonly Dictionary<EAudioType, AsyncOperationHandle<AudioClip>> _bgmHandles = new Dictionary<EAudioType, AsyncOperationHandle<AudioClip>>();
    private bool _isBGMPaused;
    private bool _hasCurrentBGM;
    private EAudioType _currentBGM;
    private bool _hasPendingBGM;
    private EAudioType _pendingBGM;
    private int _bgmRequestVersion;

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

        foreach (AsyncOperationHandle<AudioClip> handle in _bgmHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _bgmHandles.Clear();
    }

    private void Start()
    {
        PlayBGM(EAudioType.TitleBGM);
    }

    public void PlayBGM(EAudioType audioType)
    {
        if (_bgmSource == null)
        {
            Debug.LogError("AudioManager의 BGM AudioSource가 할당되지 않음.", this);
            return;
        }

        if (_hasCurrentBGM && _currentBGM == audioType && _bgmSource.clip != null)
        {
            _bgmRequestVersion++;
            _hasPendingBGM = false;

            if (_isBGMPaused)
            {
                ResumeBGM();
            }
            else if (!_bgmSource.isPlaying)
            {
                _bgmSource.Play();
            }

            return;
        }

        if (_hasPendingBGM && _pendingBGM == audioType)
        {
            return;
        }

        _pendingBGM = audioType;
        _hasPendingBGM = true;
        int requestVersion = ++_bgmRequestVersion;
        StartCoroutine(LoadAndPlayBGM(audioType, requestVersion));
    }

    public IEnumerator PrepareBGMAsync(EAudioType audioType, Action<float> onProgress = null)
    {
        yield return LoadBGMClipAsync(audioType, onProgress, null);
    }

    private IEnumerator LoadAndPlayBGM(EAudioType audioType, int requestVersion)
    {
        AudioClip clip = null;

        yield return LoadBGMClipAsync(audioType, null, loadedClip => clip = loadedClip);

        if (requestVersion != _bgmRequestVersion)
        {
            if (!_hasCurrentBGM || _currentBGM != audioType)
            {
                ReleaseBGMHandle(audioType);
            }

            yield break;
        }

        _hasPendingBGM = false;

        if (clip != null)
        {
            AssignAndPlayBGM(audioType, clip);
        }
    }

    private IEnumerator LoadBGMClipAsync(EAudioType audioType, Action<float> onProgress, Action<AudioClip> onCompleted)
    {
        AudioData data = FindAudioData(audioType);

        if (data == null)
        {
            Debug.LogError($"AudioType 매핑을 찾을 수 없음: {audioType}", this);
            onProgress?.Invoke(1f);
            onCompleted?.Invoke(null);

            yield break;
        }

        if (data.AudioClip != null)
        {
            onProgress?.Invoke(1f);
            onCompleted?.Invoke(data.AudioClip);

            yield break;
        }

        if (data.AudioClipReference == null || !data.AudioClipReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"BGM 주소 지정 참조가 누락: {audioType}", this);
            onProgress?.Invoke(1f);
            onCompleted?.Invoke(null);

            yield break;
        }

        if (!_bgmHandles.TryGetValue(audioType, out AsyncOperationHandle<AudioClip> handle) || !handle.IsValid())
        {
            handle = Addressables.LoadAssetAsync<AudioClip>(data.AudioClipReference);
            _bgmHandles[audioType] = handle;
        }

        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete * 0.9f);

            yield return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"BGM 어드레서블(Addressable)을 불러오는 데 실패: {audioType}", this);
            onProgress?.Invoke(1f);
            onCompleted?.Invoke(null);

            yield break;
        }

        AudioClip clipResult = handle.Result;

        if (clipResult.loadState == AudioDataLoadState.Unloaded)
        {
            clipResult.LoadAudioData();
        }

        while (clipResult.loadState == AudioDataLoadState.Loading)
        {
            onProgress?.Invoke(0.95f);

            yield return null;
        }

        if (clipResult.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"BGM 오디오 데이터를 불러오지 못함: {audioType}", this);
            onProgress?.Invoke(1f);
            onCompleted?.Invoke(null);

            yield break;
        }

        onProgress?.Invoke(1f);
        onCompleted?.Invoke(clipResult);
    }

    private void AssignAndPlayBGM(EAudioType audioType, AudioClip clip)
    {
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        ApplyBGMVolume();
        _bgmSource.Play();
        _currentBGM = audioType;
        _hasCurrentBGM = true;
        _isBGMPaused = false;
        _hasPendingBGM = false;
        ReleaseUnusedBGMHandles(audioType);
    }

    private void ReleaseUnusedBGMHandles(EAudioType keepType)
    {
        List<EAudioType> releaseTypes = new List<EAudioType>();
        foreach (KeyValuePair<EAudioType, AsyncOperationHandle<AudioClip>> pair in _bgmHandles)
        {
            if (pair.Key == keepType || !pair.Value.IsDone)
            {
                continue;
            }

            if (pair.Value.IsValid())
            {
                Addressables.Release(pair.Value);
            }

            releaseTypes.Add(pair.Key);
        }

        for (int i = 0; i < releaseTypes.Count; i++)
        {
            _bgmHandles.Remove(releaseTypes[i]);
        }
    }

    private void ReleaseBGMHandle(EAudioType audioType)
    {
        if (!_bgmHandles.TryGetValue(audioType, out AsyncOperationHandle<AudioClip> handle) || !handle.IsDone)
        {
            return;
        }

        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }

        _bgmHandles.Remove(audioType);
    }

    public void StopBGM()
    {
        _bgmRequestVersion++;
        _hasPendingBGM = false;

        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }

        _isBGMPaused = false;
    }

    public void PauseBGM()
    {
        if (_bgmSource != null)
        {
            _bgmSource.Pause();
        }

        _isBGMPaused = true;
    }

    public void ResumeBGM()
    {
        if (_bgmSource != null && _bgmSource.clip != null)
        {
            if (_isBGMPaused)
            {
                _bgmSource.UnPause();
            }
            else if (!_bgmSource.isPlaying)
            {
                _bgmSource.Play();
            }

            _isBGMPaused = false;
        }
    }

    public void PlaySFX(EAudioType audioType)
    {
        if (_sfxSource == null)
        {
            Debug.LogError("AudioManager의 SFX AudioSource가 할당되지 않음.", this);
            return;
        }

        AudioClip clip = FindAudioClip(audioType);

        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyBGMVolume();
    }

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

    public void RequestAudio(EAudioType audioType)
    {
        OnAudioRequested?.Invoke(audioType);
    }

    private AudioData FindAudioData(EAudioType audioType)
    {
        if (_audioData == null)
        {
            return null;
        }

        for (int i = 0; i < _audioData.Length; i++)
        {
            if (_audioData[i] != null && _audioData[i].AudioType == audioType)
            {
                return _audioData[i];
            }
        }

        return null;
    }

    private AudioClip FindAudioClip(EAudioType audioType)
    {
        AudioData data = FindAudioData(audioType);

        if (data != null && data.AudioClip != null)
        {
            return data.AudioClip;
        }

        Debug.LogError($"AudioType 클립을 찾을 수 없음: {audioType}", this);

        return null;
    }

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
}
