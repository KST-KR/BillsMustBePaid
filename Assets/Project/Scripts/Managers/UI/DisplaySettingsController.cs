using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 해상도와 화면 모드 설정을 적용하고 저장함
/// </summary>
public sealed class DisplaySettingsController : MonoBehaviour
{
    #region 상수

    private const string CResolutionWidthKey = "DisplayResolutionWidth";
    private const string CResolutionHeightKey = "DisplayResolutionHeight";
    private const string CFullscreenKey = "DisplayFullscreen";

    #endregion

    #region 내부 변수

    private static DisplaySettingsController _instance;

    #endregion

    #region 인스펙터

    [Header("해상도 설정")]
    [SerializeField] private Dropdown _legacyDisplayDropdown;
    [SerializeField] private TMP_Dropdown _tmpDisplayDropdown;

    [Header("화면 모드 설정")]
    [SerializeField] private Dropdown _legacyDisplayModeDropdown;
    [SerializeField] private TMP_Dropdown _tmpDisplayModeDropdown;

    #endregion

    #region 이벤트 함수

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        WarnIfReferencesMissing();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }

        RemoveResolutionListener();
        RemoveDisplayModeListener();
    }

    #endregion

    #region 커스텀 Private

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindDropdownsToScene(scene);
        InitializeDropdownValues();
        Debug.Log("화면 설정 준비 " + scene.name + " 해상도 " + HasResolutionDropdown() + " 모드 " + HasDisplayModeDropdown());
        StartCoroutine(LogAppliedResolution(scene.name));
    }

    private void BindDropdownsToScene(Scene scene)
    {
        RemoveResolutionListener();
        RemoveDisplayModeListener();

        bool resolutionReferenceIsCurrent =
            (_tmpDisplayDropdown != null && _tmpDisplayDropdown.gameObject.scene == scene) ||
            (_legacyDisplayDropdown != null && _legacyDisplayDropdown.gameObject.scene == scene);
        if (!resolutionReferenceIsCurrent)
        {
            _tmpDisplayDropdown = null;
            _legacyDisplayDropdown = null;
            Transform resolutionTransform = FindObjectInScene(scene, "ResolutionDropdown", null);
            _tmpDisplayDropdown = resolutionTransform != null ? resolutionTransform.GetComponent<TMP_Dropdown>() : null;
            _legacyDisplayDropdown = resolutionTransform != null ? resolutionTransform.GetComponent<Dropdown>() : null;
        }

        bool displayModeReferenceIsCurrent =
            (_tmpDisplayModeDropdown != null && _tmpDisplayModeDropdown.gameObject.scene == scene) ||
            (_legacyDisplayModeDropdown != null && _legacyDisplayModeDropdown.gameObject.scene == scene);
        if (!displayModeReferenceIsCurrent)
        {
            _tmpDisplayModeDropdown = null;
            _legacyDisplayModeDropdown = null;
            Transform displayModeTransform = FindObjectInScene(scene, "DisplayModeDropdown", "DisplayModeRow");
            _tmpDisplayModeDropdown = displayModeTransform != null ? displayModeTransform.GetComponent<TMP_Dropdown>() : null;
            _legacyDisplayModeDropdown = displayModeTransform != null ? displayModeTransform.GetComponent<Dropdown>() : null;
        }
    }

    private static Transform FindObjectInScene(Scene scene, string objectName, string requiredAncestorName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate.name != objectName ||
                    (candidate.GetComponent<TMP_Dropdown>() == null && candidate.GetComponent<Dropdown>() == null))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(requiredAncestorName) || HasAncestor(candidate, requiredAncestorName))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool HasAncestor(Transform transform, string ancestorName)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name == ancestorName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private IEnumerator LogAppliedResolution(string sceneName)
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("씬 " + sceneName + " 화면 " + Screen.width + "x" + Screen.height +
                  (Screen.fullScreen ? " 전체 화면" : " 창 모드"));
    }

    private void InitializeDropdownValues()
    {
        if (HasResolutionDropdown())
        {
            RemoveResolutionListener();
            AddResolutionListener();
        }

        if (HasDisplayModeDropdown())
        {
            RemoveDisplayModeListener();
            AddDisplayModeListener();
        }

        bool hasSavedSettings = PlayerPrefs.HasKey(CResolutionWidthKey) &&
                                PlayerPrefs.HasKey(CResolutionHeightKey) &&
                                PlayerPrefs.HasKey(CFullscreenKey);
        if (hasSavedSettings)
        {
            int savedWidth = PlayerPrefs.GetInt(CResolutionWidthKey);
            int savedHeight = PlayerPrefs.GetInt(CResolutionHeightKey);
            bool savedFullscreen = PlayerPrefs.GetInt(CFullscreenKey) != 0;
            int resolutionIndex = FindResolutionIndex(savedWidth, savedHeight);

            if (HasResolutionDropdown() && resolutionIndex >= 0)
            {
                SetResolutionDropdownValue(resolutionIndex);
            }

            if (HasDisplayModeDropdown())
            {
                SetDisplayModeDropdownValue(savedFullscreen ? 0 : 1);
            }

            ApplyResolution(savedWidth, savedHeight, savedFullscreen);
            return;
        }

        if (!HasResolutionDropdown())
        {
            return;
        }

        int resolutionIndexFromScreen = FindResolutionIndex(Screen.width, Screen.height);
        if (resolutionIndexFromScreen >= 0)
        {
            SetResolutionDropdownValue(resolutionIndexFromScreen);
        }
        else if (GetResolutionOptionCount() > 0)
        {
            resolutionIndexFromScreen = Mathf.Clamp(GetResolutionDropdownValue(), 0, GetResolutionOptionCount() - 1);
        }

        bool isFullscreen = Screen.fullScreen;
        if (HasDisplayModeDropdown())
        {
            SetDisplayModeDropdownValue(isFullscreen ? 0 : 1);
        }

        if (resolutionIndexFromScreen >= 0 &&
            TryParseResolution(GetResolutionOptionText(resolutionIndexFromScreen), out int width, out int height))
        {
            ApplyResolution(width, height, isFullscreen);
        }
    }

    private void WarnIfReferencesMissing()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }

        if (!HasResolutionDropdown())
        {
            Debug.LogError("해상도 Dropdown 연결 안 됨", this);
        }

        if (!HasDisplayModeDropdown())
        {
            Debug.LogError("화면 모드 Dropdown 연결 안 됨", this);
        }
    }

    private int FindResolutionIndex(int width, int height)
    {
        if (!HasResolutionDropdown())
        {
            return -1;
        }

        for (int i = 0; i < GetResolutionOptionCount(); i++)
        {
            if (TryParseResolution(GetResolutionOptionText(i), out int optionWidth, out int optionHeight) &&
                optionWidth == width && optionHeight == height)
            {
                return i;
            }
        }

        return -1;
    }

    private void HandleResolutionChanged(int optionIndex)
    {
        Debug.Log("해상도 변경 이벤트 " + optionIndex);
        if (!HasResolutionDropdown() || optionIndex < 0 || optionIndex >= GetResolutionOptionCount())
        {
            return;
        }

        string optionText = GetResolutionOptionText(optionIndex);
        if (!TryParseResolution(optionText, out int width, out int height))
        {
            Debug.LogWarning("해상도 옵션 오류 " + optionText);
            return;
        }

        ApplyResolution(width, height, IsFullscreenSelected());
    }

    private void HandleDisplayModeChanged(int optionIndex)
    {
        Debug.Log("화면 모드 변경 이벤트 " + optionIndex);
        if (!HasResolutionDropdown() || GetResolutionOptionCount() == 0)
        {
            return;
        }

        string optionText = GetResolutionOptionText(GetResolutionDropdownValue());
        if (TryParseResolution(optionText, out int width, out int height))
        {
            ApplyResolution(width, height, optionIndex == 0);
        }
    }

    private bool IsFullscreenSelected()
    {
        return !HasDisplayModeDropdown() || GetDisplayModeDropdownValue() == 0;
    }

    private bool HasResolutionDropdown()
    {
        return _tmpDisplayDropdown != null || _legacyDisplayDropdown != null;
    }

    private bool HasDisplayModeDropdown()
    {
        return _tmpDisplayModeDropdown != null || _legacyDisplayModeDropdown != null;
    }

    private int GetResolutionOptionCount()
    {
        return _tmpDisplayDropdown != null ? _tmpDisplayDropdown.options.Count :
            (_legacyDisplayDropdown != null ? _legacyDisplayDropdown.options.Count : 0);
    }

    private string GetResolutionOptionText(int index)
    {
        return _tmpDisplayDropdown != null ? _tmpDisplayDropdown.options[index].text :
            (_legacyDisplayDropdown != null ? _legacyDisplayDropdown.options[index].text : string.Empty);
    }

    private int GetResolutionDropdownValue()
    {
        return _tmpDisplayDropdown != null ? _tmpDisplayDropdown.value :
            (_legacyDisplayDropdown != null ? _legacyDisplayDropdown.value : 0);
    }

    private void SetResolutionDropdownValue(int value)
    {
        if (_tmpDisplayDropdown != null)
        {
            _tmpDisplayDropdown.SetValueWithoutNotify(value);
        }
        else if (_legacyDisplayDropdown != null)
        {
            _legacyDisplayDropdown.SetValueWithoutNotify(value);
        }
    }

    private int GetDisplayModeDropdownValue()
    {
        return _tmpDisplayModeDropdown != null ? _tmpDisplayModeDropdown.value :
            (_legacyDisplayModeDropdown != null ? _legacyDisplayModeDropdown.value : 0);
    }

    private void SetDisplayModeDropdownValue(int value)
    {
        if (_tmpDisplayModeDropdown != null)
        {
            _tmpDisplayModeDropdown.SetValueWithoutNotify(value);
        }
        else if (_legacyDisplayModeDropdown != null)
        {
            _legacyDisplayModeDropdown.SetValueWithoutNotify(value);
        }
    }

    private void AddResolutionListener()
    {
        if (_tmpDisplayDropdown != null)
        {
            _tmpDisplayDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }
        else if (_legacyDisplayDropdown != null)
        {
            _legacyDisplayDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }
    }

    private void RemoveResolutionListener()
    {
        if (_tmpDisplayDropdown != null)
        {
            _tmpDisplayDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
        }

        if (_legacyDisplayDropdown != null)
        {
            _legacyDisplayDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
        }
    }

    private void AddDisplayModeListener()
    {
        if (_tmpDisplayModeDropdown != null)
        {
            _tmpDisplayModeDropdown.onValueChanged.AddListener(HandleDisplayModeChanged);
        }
        else if (_legacyDisplayModeDropdown != null)
        {
            _legacyDisplayModeDropdown.onValueChanged.AddListener(HandleDisplayModeChanged);
        }
    }

    private void RemoveDisplayModeListener()
    {
        if (_tmpDisplayModeDropdown != null)
        {
            _tmpDisplayModeDropdown.onValueChanged.RemoveListener(HandleDisplayModeChanged);
        }

        if (_legacyDisplayModeDropdown != null)
        {
            _legacyDisplayModeDropdown.onValueChanged.RemoveListener(HandleDisplayModeChanged);
        }
    }

    private void ApplyResolution(int width, int height, bool fullscreen)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        Debug.Log("해상도 적용 " + width + "x" + height + (fullscreen ? " 전체 화면" : " 창 모드"));
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
        PlayerPrefs.SetInt(CResolutionWidthKey, width);
        PlayerPrefs.SetInt(CResolutionHeightKey, height);
        PlayerPrefs.SetInt(CFullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static bool TryParseResolution(string optionText, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(optionText))
        {
            return false;
        }

        // 숫자 x 숫자 형식의 해상도 문자열을 정규식으로 추출
        Match match = Regex.Match(optionText, @"(\d+)\s*[^0-9]+\s*(\d+)");
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups[1].Value, out width) &&
               int.TryParse(match.Groups[2].Value, out height);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettingsBeforeSceneLoad()
    {
        if (!PlayerPrefs.HasKey(CResolutionWidthKey) ||
            !PlayerPrefs.HasKey(CResolutionHeightKey) ||
            !PlayerPrefs.HasKey(CFullscreenKey))
        {
            return;
        }

        int width = PlayerPrefs.GetInt(CResolutionWidthKey);
        int height = PlayerPrefs.GetInt(CResolutionHeightKey);
        bool fullscreen = PlayerPrefs.GetInt(CFullscreenKey) != 0;
        if (width > 0 && height > 0)
        {
            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
        }
    }

    #endregion
}
