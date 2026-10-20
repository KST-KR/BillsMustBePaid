using UnityEngine;

/// <summary>
/// [UI-01-01]
/// 메인 메뉴 UI 표시 및 패널 전환 관리
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("저장 데이터에 따라 표시할 버튼")]
    [SerializeField] private GameObject _continueButton;
    [SerializeField] private GameObject _skillTreeButton;
    [SerializeField] private GameObject _loadOrNewGameButton;

    [Header("메뉴 패널")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _loadPanel;
    [SerializeField] private GameObject _settingsPanel;

    private void Start()
    {
        bool hasSaveData = CheckSaveData();

        SetSaveDataUI(hasSaveData);

        _mainMenuPanel.SetActive(true);
        _loadPanel.SetActive(false);
        _settingsPanel.SetActive(false);
    }

    private bool CheckSaveData()
    {
        // TODO: 저장 시스템 완성 후 실제 데이터 확인
        return false;
    }

    /// <summary>
    /// 저장 데이터 유무에 따른 버튼 표시
    /// </summary>
    private void SetSaveDataUI(bool hasSaveData)
    {
        _continueButton.SetActive(hasSaveData);
        _skillTreeButton.SetActive(hasSaveData);

        // 저장 데이터가 없어도 새 게임 진입은 가능해야 함
        _loadOrNewGameButton.SetActive(true);
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
