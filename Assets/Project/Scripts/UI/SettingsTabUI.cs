using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 메뉴의 탭 전환과 선택 상태 UI를 관리합니다.
/// </summary>
public class SettingsTabUI : MonoBehaviour
{
    #region Inspector

    [Header("Tab Buttons")]
    [SerializeField] private Button _generalTabButton;
    [SerializeField] private Button _cursorTabButton;
    [SerializeField] private Button _gameplayTabButton;

    [Header("Tab Contents")]
    [SerializeField] private GameObject _generalContent;
    [SerializeField] private GameObject _cursorContent;
    [SerializeField] private GameObject _gameplayContent;

    [Header("Tab Underlines")]
    [SerializeField] private GameObject _generalUnderline;
    [SerializeField] private GameObject _cursorUnderline;
    [SerializeField] private GameObject _gameplayUnderline;

    [Header("Tab Text")]
    [SerializeField] private TMP_Text _generalTabText;
    [SerializeField] private TMP_Text _cursorTabText;
    [SerializeField] private TMP_Text _gameplayTabText;

    [Header("Tab Colors")]
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _normalColor = new Color(0.784f, 0.749f, 0.710f, 1f);

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _generalTabButton.onClick.AddListener(ShowGeneralTab);
        _cursorTabButton.onClick.AddListener(ShowCursorTab);
        _gameplayTabButton.onClick.AddListener(ShowGameplayTab);
    }

    private void OnEnable()
    {
        ShowGeneralTab();
    }

    private void OnDestroy()
    {
        _generalTabButton.onClick.RemoveListener(ShowGeneralTab);
        _cursorTabButton.onClick.RemoveListener(ShowCursorTab);
        _gameplayTabButton.onClick.RemoveListener(ShowGameplayTab);
    }

    #endregion

    #region Tab Control

    /// <summary>
    /// 일반 설정 탭을 표시합니다.
    /// </summary>
    public void ShowGeneralTab()
    {
        SetActiveTab(0);
    }

    /// <summary>
    /// 커서 설정 탭을 표시합니다.
    /// </summary>
    public void ShowCursorTab()
    {
        SetActiveTab(1);
    }

    /// <summary>
    /// 게임플레이 설정 탭을 표시합니다.
    /// </summary>
    public void ShowGameplayTab()
    {
        SetActiveTab(2);
    }

    /// <summary>
    /// 선택된 탭에 따라 콘텐츠와 시각 효과를 갱신합니다.
    /// </summary>
    private void SetActiveTab(int tabIndex)
    {
        bool isGeneral = tabIndex == 0;
        bool isCursor = tabIndex == 1;
        bool isGameplay = tabIndex == 2;

        // 탭별 콘텐츠 표시
        _generalContent.SetActive(isGeneral);
        _cursorContent.SetActive(isCursor);
        _gameplayContent.SetActive(isGameplay);

        // 선택된 탭의 금색 밑줄 표시
        _generalUnderline.SetActive(isGeneral);
        _cursorUnderline.SetActive(isCursor);
        _gameplayUnderline.SetActive(isGameplay);

        // 선택된 탭의 글씨 색상 변경
        _generalTabText.color = isGeneral ? _selectedColor : _normalColor;

        _cursorTabText.color = isCursor ? _selectedColor : _normalColor;

        _gameplayTabText.color = isGameplay ? _selectedColor : _normalColor;
    }

    #endregion
}

