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
    public float CursorSpeed => _settings.cursorSpeed;
    public float CursorSize => _settings.cursorSize;
    public float CursorColor => _settings.cursorColor;
    public float CursorOpacity => _settings.cursorOpacity;
    public bool DamageTextEnabled => _settings.damageTextMode == 0;
    public bool CoinEnabled => _settings.coinEnabled;
    public int FragmentAmount => _settings.fragmentAmount;
    #endregion

    #region 이벤트
    public event Action OnSettingsChanged;
    #endregion

    #region 내부 변수
    private SaveSettingData _settings;
    [SerializeField] private Texture2D _cursorSourceTexture;
    private Texture2D _runtimeCursorTexture;
    private float _appliedCursorSize = -1f;
    private float _appliedCursorColor = -1f;
    private float _appliedCursorOpacity = -1f;
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
        if (_cursorSourceTexture == null)
        {
            Debug.LogWarning("커서 텍스처 없음");
        }
        ApplyCursor();
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
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (_runtimeCursorTexture != null)
        {
            Destroy(_runtimeCursorTexture);
            _runtimeCursorTexture = null;
        }
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

        ApplyCursor();
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
        ApplyCursor();
        OnSettingsChanged?.Invoke();
    }

    public void SetCursorSourceTexture(Texture2D texture)
    {
        if (texture == null || _cursorSourceTexture == texture)
        {
            return;
        }

        _cursorSourceTexture = texture;
        _appliedCursorSize = -1f;
        _appliedCursorColor = -1f;
        _appliedCursorOpacity = -1f;
        ApplyCursor();
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

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && _settings != null)
        {
            ApplyCursor();
        }
    }

    /// <summary>
    /// 실시간으로 커서 텍스처를 생성함 (설정도 적용)
    /// </summary>
    private void ApplyCursor()
    {
        if (_settings == null)
        {
            return;
        }

        if (_cursorSourceTexture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        if (_runtimeCursorTexture != null &&
            Mathf.Approximately(_appliedCursorSize, _settings.cursorSize) &&
            Mathf.Approximately(_appliedCursorColor, _settings.cursorColor) &&
            Mathf.Approximately(_appliedCursorOpacity, _settings.cursorOpacity))
        {
            Cursor.SetCursor(_runtimeCursorTexture, Vector2.zero, CursorMode.ForceSoftware);
            return;
        }

        if (_runtimeCursorTexture != null)
        {
            Destroy(_runtimeCursorTexture);
        }

        float sizeScale = Mathf.Lerp(1f, 2f, _settings.cursorSize);
        int width = Mathf.Max(1, Mathf.RoundToInt(_cursorSourceTexture.width * sizeScale));
        int height = Mathf.Max(1, Mathf.RoundToInt(_cursorSourceTexture.height * sizeScale));
        _runtimeCursorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "RuntimeCursor",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        // 커서 밝기 조절
        Color hueTint = Color.HSVToRGB(_settings.cursorColor, 1f, 1f);
        Color tint = Color.Lerp(Color.white, hueTint, 0.35f);
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = _cursorSourceTexture.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                pixel.r = Mathf.Clamp01(pixel.r * tint.r * 1.5f);
                pixel.g = Mathf.Clamp01(pixel.g * tint.g * 1.5f);
                pixel.b = Mathf.Clamp01(pixel.b * tint.b * 1.5f);
                pixel.a *= _settings.cursorOpacity;
                pixels[y * width + x] = pixel;
            }
        }

        _runtimeCursorTexture.SetPixels32(pixels);
        _runtimeCursorTexture.Apply(false, true);
        _appliedCursorSize = _settings.cursorSize;
        _appliedCursorColor = _settings.cursorColor;
        _appliedCursorOpacity = _settings.cursorOpacity;
        Cursor.SetCursor(_runtimeCursorTexture, Vector2.zero, CursorMode.ForceSoftware);
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
        _settings.fragmentAmount = Mathf.Clamp(_settings.fragmentAmount, 0, 3);
        _settings.damageTextMode = Mathf.Clamp(_settings.damageTextMode, 0, 1);
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
            coinEnabled = settings.coinEnabled,
            fragmentAmount = settings.fragmentAmount
        };
    }
    #endregion
}
