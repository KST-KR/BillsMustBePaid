using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameLegacyShop 씬의 일시정지 및 설정 패널을 관리
/// </summary>
public sealed class LegacyShopPauseController : MonoBehaviour
{
    [Header("일시정지 UI")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsBackButton;
    [SerializeField] private GameObject _mainConfirmPanel;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _mainConfirmYesButton;
    [SerializeField] private Button _mainConfirmNoButton;
    [SerializeField] private Button _quitButton;

    private bool _isPaused;
    private float _previousTimeScale = 1f;
    private LegacyShopRingHover[] _shopHovers;

    private void Awake()
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
        }

        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }

        if (_mainConfirmPanel != null)
        {
            _mainConfirmPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (_settingsButton != null)
        {
            _settingsButton.onClick.AddListener(OpenSettings);
        }

        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(Resume);
        }

        if (_settingsBackButton != null)
        {
            _settingsBackButton.onClick.AddListener(CloseSettings);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(ShowMainConfirmPanel);
        }

        if (_mainConfirmYesButton != null)
        {
            _mainConfirmYesButton.onClick.AddListener(MainConfirm);
        }

        if (_mainConfirmNoButton != null)
        {
            _mainConfirmNoButton.onClick.AddListener(CloseMainConfirmPanel);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void OnDisable()
    {
        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveListener(Resume);
        }

        if (_settingsBackButton != null)
        {
            _settingsBackButton.onClick.RemoveListener(CloseSettings);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(ShowMainConfirmPanel);
        }

        if (_mainConfirmYesButton != null)
        {
            _mainConfirmYesButton.onClick.RemoveListener(MainConfirm);
        }

        if (_mainConfirmNoButton != null)
        {
            _mainConfirmNoButton.onClick.RemoveListener(CloseMainConfirmPanel);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(QuitGame);
        }

        SetShopHoverEnabled(true);
        RestoreTimeScale();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (_settingsPanel != null && _settingsPanel.activeSelf)
        {
            CloseSettings();
        }
        else if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    private static Transform FindDescendant(Transform parent, string targetName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == targetName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDescendant(parent.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void Pause()
    {
        if (_pausePanel == null)
        {
            return;
        }

        _previousTimeScale = Time.timeScale;
        _isPaused = true;
        Time.timeScale = 0f;
        SetShopHoverEnabled(false);
        _pausePanel.SetActive(true);
    }

    private void Resume()
    {
        AudioManager.Instance?.PlaySFX(EAudioType.MenuSelect);
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }

        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
        }

        SetShopHoverEnabled(true);
        RestoreTimeScale();
    }

    private void OpenSettings()
    {
        if (_settingsPanel == null)
        {
            return;
        }

        _settingsPanel.SetActive(true);
        _settingsPanel.transform.SetAsLastSibling();
    }

    private void CloseSettings()
    {
        AudioManager.Instance?.PlaySFX(EAudioType.MenuSelect);
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }
    }

    private void ShowMainConfirmPanel()
    {
        AudioManager.Instance?.PlaySFX(EAudioType.MenuSelect);
        if (_mainConfirmPanel != null)
        {
            _mainConfirmPanel.SetActive(true);
        }
    }

    private void CloseMainConfirmPanel()
    {
        AudioManager.Instance?.PlaySFX(EAudioType.MenuSelect);
        if (_mainConfirmPanel != null)
        {
            _mainConfirmPanel.SetActive(false);
        }
    }

    private void MainConfirm()
    {
        AudioManager.Instance?.PlaySFX(EAudioType.MenuSelect);
        RestoreTimeScale();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToTitle();
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager != null && transitionManager.LoadMainMenuScene())
        {
            return;
        }

        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void QuitGame()
    {
        AudioManager.Instance?.PlaySFX(EAudioType.MenuSelect);

        Debug.Log("게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetShopHoverEnabled(bool isEnabled)
    {
        if (_shopHovers == null)
        {
            return;
        }

        for (int i = 0; i < _shopHovers.Length; i++)
        {
            if (_shopHovers[i] != null)
            {
                _shopHovers[i].SetInteractionEnabled(isEnabled);
            }
        }
    }

    private void RestoreTimeScale()
    {
        if (!_isPaused)
        {
            return;
        }

        Time.timeScale = _previousTimeScale;
        _isPaused = false;
    }
}
