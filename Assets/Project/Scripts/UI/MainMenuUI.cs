using UnityEngine;

/// <summary>
/// [UI-01-01]
/// 메인 메뉴 UI 표시 및 패널 전환 관리
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("항상 표시할 버튼")]
    [SerializeField] private GameObject _settingsButton;
    [SerializeField] private GameObject _exitButton;

    [Header("저장 데이터가 없을 때 표시할 버튼")]
    [SerializeField] private GameObject _newGameButton;

    [Header("저장 데이터가 있을 때 표시할 버튼")]
    [SerializeField] private GameObject _continueButton;
    [SerializeField] private GameObject _skillTreeButton;
    [SerializeField] private GameObject _loadOrNewGameButton;

    [Header("메뉴 패널")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _loadPanel;
    [SerializeField] private GameObject _settingsPanel;

    private void Start()
    {
        // 실제 저장 시스템과 연결할 부분
        bool hasSaveData = CheckSaveData();

        SetSaveDataUI(hasSaveData);

        // 초기 패널 설정
        _mainMenuPanel.SetActive(true);
        _loadPanel.SetActive(false);
        _settingsPanel.SetActive(false);
    }

    private bool CheckSaveData()
    {
        // 나중에 실제 저장 데이터 확인 로직으로 교체
        return true;
    }

    /// <summary>
    /// 저장 데이터 유무에 따라 버튼 표시
    /// </summary>
    private void SetSaveDataUI(bool hasSaveData)
    {
        // 항상 표시할 버튼
        _settingsButton.SetActive(true);
        _exitButton.SetActive(true);

        // 저장 데이터가 없으면 새 게임 버튼 표시
        _newGameButton.SetActive(!hasSaveData);

        // 저장 데이터가 있으면 관련 버튼 표시
        _continueButton.SetActive(hasSaveData);
        _skillTreeButton.SetActive(hasSaveData);
        _loadOrNewGameButton.SetActive(hasSaveData);
    }

    /// <summary>
    /// 불러오기 패널 열기
    /// </summary>
    public void OpenLoadPanel()
    {
        _mainMenuPanel.SetActive(false);
        _loadPanel.SetActive(true);
    }

    /// <summary>
    /// 불러오기 패널 닫고 메인 메뉴 복귀
    /// </summary>
    public void CloseLoadPanel()
    {
        _loadPanel.SetActive(false);
        _mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// 설정 패널 열기
    /// </summary>
    public void OpenSettingsPanel()
    {
        _mainMenuPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    /// <summary>
    /// 설정 패널 닫고 메인 메뉴 복귀
    /// </summary>
    public void CloseSettingsPanel()
    {
        _settingsPanel.SetActive(false);
        _mainMenuPanel.SetActive(true);
    }
}