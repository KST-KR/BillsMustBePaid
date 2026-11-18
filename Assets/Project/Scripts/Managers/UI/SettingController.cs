using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬별 설정 UI와 사용자 설정을 연결
/// </summary>
public sealed class SettingController : MonoBehaviour
{
    [Header("커서 텍스처")]
    [SerializeField] private Texture2D _cursorSourceTexture;

    #region 내부 변수
    private SettingsManager _manager;
    private readonly List<Action> _removeListeners = new List<Action>();
    private readonly List<Action> _refreshControls = new List<Action>();
    private TMP_Dropdown _resolutionDropdown;
    private TMP_Dropdown _displayModeDropdown;
    #endregion

    #region 이벤트 함수
    private void OnDestroy()
    {
        ClearBindings();
    }
    #endregion

    #region 커스텀 메서드 (Public)
    public static void BindScene(Scene scene, SettingsManager manager)
    {
        if (manager == null || !scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        SettingController controller = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            SettingController[] sceneControllers = root.GetComponentsInChildren<SettingController>(true);
            if (sceneControllers.Length > 0)
            {
                controller = sceneControllers[0];
                break;
            }
        }

        if (controller != null && controller._cursorSourceTexture != null)
        {
            manager.SetCursorSourceTexture(controller._cursorSourceTexture);
        }

        if (controller == null)
        {
            controller = manager.GetComponent<SettingController>();
        }

        if (controller != null)
        {
            controller.Bind(scene, manager);
        }
    }
    #endregion

    #region 커스텀 메서드 (Private)
    private void Bind(Scene scene, SettingsManager manager)
    {
        ClearBindings();
        _manager = manager;

        Transform panel = FindSettingsPanel(scene);
        if (panel == null)
        {
            return;
        }

        Transform resolutionRow = Find(panel, "ResolutionRow");
        Transform displayRow = Find(panel, "DisplayModeRow");
        _resolutionDropdown = FindDropdown(resolutionRow, "ResolutionDropdown");
        _displayModeDropdown = FindDropdown(displayRow, "DisplayModeDropdown");

        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }
        if (_displayModeDropdown != null)
        {
            _displayModeDropdown.onValueChanged.AddListener(HandleDisplayModeChanged);
        }

        BindSlider(panel, "MasterSlider", s => s.masterVolume, (s, v) => s.masterVolume = v);
        BindSlider(panel, "MusicSlider", s => s.musicVolume, (s, v) => s.musicVolume = v);
        BindSlider(panel, "SFXSlider", s => s.sfxVolume, (s, v) => s.sfxVolume = v);
        BindSlider(panel, "CursorSpeedSlider", s => s.cursorSpeed, (s, v) => s.cursorSpeed = v);
        BindSlider(panel, "CursorSizeSlider", s => s.cursorSize, (s, v) => s.cursorSize = v);
        BindSlider(panel, "CursorColorSlider", s => s.cursorColor, (s, v) => s.cursorColor = v);
        BindSlider(panel, "CursorOpacitySlider", s => s.cursorOpacity, (s, v) => s.cursorOpacity = v);
        BindDropdown(panel, "CoinRow", "CoinDropdown", s => s.coinEnabled ? 0 : 1, (s, v) => s.coinEnabled = v == 0);
        BindSlider(panel, "FragmentSlider", s => s.fragmentAmount, (s, v) => s.fragmentAmount = (int)v);
        BindDropdown(panel, "DamageTextRow", "DamageTextDropdown", s => s.damageTextMode, (s, v) => s.damageTextMode = v);
        BindDropdown(panel, "MoneyTextRow", "DisplayModeDropdown", s => s.moneyTextMode, (s, v) => s.moneyTextMode = v);
        _manager.OnSettingsChanged += RefreshControls;
        RefreshControls();
    }

    private void ClearBindings()
    {
        if (_manager != null)
        {
            _manager.OnSettingsChanged -= RefreshControls;
        }
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
        }
        if (_displayModeDropdown != null)
        {
            _displayModeDropdown.onValueChanged.RemoveListener(HandleDisplayModeChanged);
        }

        foreach (Action remove in _removeListeners)
        {
            remove();
        }
        _removeListeners.Clear();
        _refreshControls.Clear();
        _resolutionDropdown = null;
        _displayModeDropdown = null;
        _manager = null;
    }

    private void RefreshControls()
    {
        SaveSettingData settings = _manager.Settings;
        if (_resolutionDropdown != null)
        {
            int selectedIndex = -1;
            for (int i = 0; i < _resolutionDropdown.options.Count; i++)
            {
                if (TryParseResolution(_resolutionDropdown.options[i].text, out int width, out int height) &&
                    width == settings.resolutionWidth && height == settings.resolutionHeight)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0)
            {
                _resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(settings.resolutionWidth + " × " + settings.resolutionHeight));
                selectedIndex = _resolutionDropdown.options.Count - 1;
            }
            _resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        }
        if (_displayModeDropdown != null && _displayModeDropdown.options.Count > 0)
        {
            _displayModeDropdown.SetValueWithoutNotify(Mathf.Min(settings.fullscreen ? 0 : 1, _displayModeDropdown.options.Count - 1));
        }

        foreach (Action refresh in _refreshControls)
        {
            refresh();
        }
    }

    private void BindSlider(Transform panel, string name, Func<SaveSettingData, float> get, Action<SaveSettingData, float> set)
    {
        Transform child = Find(panel, name);
        Slider slider = child != null ? child.GetComponent<Slider>() : null;
        if (slider == null)
        {
            return;
        }

        UnityAction<float> changed = value =>
        {
            SaveSettingData settings = _manager.ExportSettings();
            set(settings, value);
            _manager.UpdateSettings(settings);
        };
        slider.onValueChanged.AddListener(changed);
        _removeListeners.Add(() => { if (slider != null) slider.onValueChanged.RemoveListener(changed); });
        _refreshControls.Add(() => { if (slider != null) slider.SetValueWithoutNotify(get(_manager.Settings)); });
    }

    private void BindDropdown(Transform panel, string rowName, string name, Func<SaveSettingData, int> get, Action<SaveSettingData, int> set)
    {
        Transform row = Find(panel, rowName);
        TMP_Dropdown dropdown = FindDropdown(row, name);
        if (dropdown == null)
        {
            return;
        }

        UnityAction<int> changed = value =>
        {
            SaveSettingData settings = _manager.ExportSettings();
            set(settings, value);
            _manager.UpdateSettings(settings);
        };
        dropdown.onValueChanged.AddListener(changed);
        _removeListeners.Add(() => { if (dropdown != null) dropdown.onValueChanged.RemoveListener(changed); });
        _refreshControls.Add(() => { if (dropdown != null && dropdown.options.Count > 0) dropdown.SetValueWithoutNotify(Mathf.Clamp(get(_manager.Settings), 0, dropdown.options.Count - 1)); });
    }

    private void HandleResolutionChanged(int index)
    {
        if (_resolutionDropdown != null && index >= 0 && index < _resolutionDropdown.options.Count &&
            TryParseResolution(_resolutionDropdown.options[index].text, out int width, out int height))
        {
            _manager.SetResolution(width, height);
        }
    }

    private void HandleDisplayModeChanged(int index)
    {
        _manager.SetFullscreen(index == 0);
    }

    private static bool TryParseResolution(string text, out int width, out int height)
    {
        width = 0;
        height = 0;
        Match match = Regex.Match(text ?? string.Empty, @"(\d+)\s*[^0-9]+\s*(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out width) &&
            int.TryParse(match.Groups[2].Value, out height) && width > 0 && height > 0;
    }

    private static Transform FindSettingsPanel(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if ((child.name == "SettingsPanel" || child.name == "PauseSettingsPanel") &&
                    Find(child, "ResolutionRow") != null)
                {
                    return child;
                }
            }
        }

        return null;
    }

    private static TMP_Dropdown FindDropdown(Transform parent, string name)
    {
        Transform child = parent != null ? Find(parent, name) : null;
        return child != null ? child.GetComponent<TMP_Dropdown>() : null;
    }

    private static Transform Find(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
    #endregion
}