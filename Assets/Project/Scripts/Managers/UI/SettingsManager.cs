using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 간 사용자 설정과 화면 적용을 관리
/// </summary>
public sealed class SettingsManager : PersistentSingleton<SettingsManager>
{
    #region 속성
    public SaveSettingData Settings => CopySettings(_settings);
    #endregion

    #region 이벤트
    public event Action OnSettingsChanged;
    #endregion

    #region 내부 변수
    private SaveSettingData _settings;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        _settings = new SaveSettingData
        {
            resolutionWidth = Screen.width,
            resolutionHeight = Screen.height,
            fullscreen = Screen.fullScreen
        };
        SceneManager.sceneLoaded += HandleSceneLoaded;
        if (gameObject.GetComponent<SettingController>() == null)
        {
            gameObject.AddComponent<SettingController>();
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            SettingController.BindScene(activeScene, this);
        }
    }

    protected override void OnSingletonDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }
    #endregion

    #region 커스텀 메서드 (Public)
    public static SettingsManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SettingsManager existing = FindObjectOfType<SettingsManager>();
        return existing != null ? existing : new GameObject("SettingsManager").AddComponent<SettingsManager>();
    }

    public void SetResolution(int width, int height)
    {
        if (width <= 0 || height <= 0 ||
            (_settings.resolutionWidth == width && _settings.resolutionHeight == height))
        {
            return;
        }

        _settings.resolutionWidth = width;
        _settings.resolutionHeight = height;
        ApplyDisplay();
        OnSettingsChanged?.Invoke();
    }

    public void SetFullscreen(bool fullscreen)
    {
        if (_settings.fullscreen == fullscreen)
        {
            return;
        }

        _settings.fullscreen = fullscreen;
        ApplyDisplay();
        OnSettingsChanged?.Invoke();
    }

    public void UpdateSettings(SaveSettingData settings)
    {
        if (settings == null)
        {
            return;
        }

        int previousWidth = _settings.resolutionWidth;
        int previousHeight = _settings.resolutionHeight;
        bool previousFullscreen = _settings.fullscreen;
        _settings = CopySettings(settings);
        NormalizeSettings();
        if (previousWidth != _settings.resolutionWidth || previousHeight != _settings.resolutionHeight ||
            previousFullscreen != _settings.fullscreen)
        {
            ApplyDisplay();
        }

        OnSettingsChanged?.Invoke();
    }

    public SaveSettingData ExportSettings()
    {
        return CopySettings(_settings);
    }

    public void ImportSettings(SaveSettingData settings)
    {
        if (settings == null)
        {
            return;
        }

        _settings = CopySettings(settings);
        NormalizeSettings();
        ApplyDisplay();
        OnSettingsChanged?.Invoke();
    }
    #endregion

    #region 커스텀 메서드 (Private)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSettings()
    {
        GetOrCreate();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if ((scene.name == "MainMenu" || scene.name == "Game") && SaveManager.Instance == null)
        {
            new GameObject("SaveManager").AddComponent<SaveManager>();
        }

        SettingController.BindScene(scene, this);
    }

    private void ApplyDisplay()
    {
        Screen.SetResolution(_settings.resolutionWidth, _settings.resolutionHeight,
            _settings.fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
    }

    private void NormalizeSettings()
    {
        if (_settings.resolutionWidth <= 0 || _settings.resolutionHeight <= 0)
        {
            _settings.resolutionWidth = Mathf.Max(1, Screen.width);
            _settings.resolutionHeight = Mathf.Max(1, Screen.height);
        }

        _settings.masterVolume = Mathf.Clamp01(_settings.masterVolume);
        _settings.musicVolume = Mathf.Clamp01(_settings.musicVolume);
        _settings.sfxVolume = Mathf.Clamp01(_settings.sfxVolume);
        _settings.cursorSpeed = Mathf.Clamp01(_settings.cursorSpeed);
        _settings.cursorSize = Mathf.Clamp01(_settings.cursorSize);
        _settings.cursorColor = Mathf.Clamp01(_settings.cursorColor);
        _settings.cursorOpacity = Mathf.Clamp01(_settings.cursorOpacity);
        _settings.coinAmount = Mathf.Clamp01(_settings.coinAmount);
        _settings.fragmentAmount = Mathf.Clamp01(_settings.fragmentAmount);
        _settings.damageTextMode = Mathf.Max(0, _settings.damageTextMode);
        _settings.moneyTextMode = Mathf.Max(0, _settings.moneyTextMode);
    }

    private static SaveSettingData CopySettings(SaveSettingData settings)
    {
        return new SaveSettingData
        {
            masterVolume = settings.masterVolume,
            musicVolume = settings.musicVolume,
            sfxVolume = settings.sfxVolume,
            resolutionWidth = settings.resolutionWidth,
            resolutionHeight = settings.resolutionHeight,
            fullscreen = settings.fullscreen,
            cursorSpeed = settings.cursorSpeed,
            cursorSize = settings.cursorSize,
            cursorColor = settings.cursorColor,
            cursorOpacity = settings.cursorOpacity,
            damageTextMode = settings.damageTextMode,
            moneyTextMode = settings.moneyTextMode,
            coinAmount = settings.coinAmount,
            fragmentAmount = settings.fragmentAmount
        };
    }
    #endregion
}
