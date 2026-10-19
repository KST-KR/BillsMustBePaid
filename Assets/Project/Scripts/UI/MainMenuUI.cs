using UnityEngine;

/// <summary>
/// [UI-01-01]
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("저장 데이터에 따라 표시할 버튼")]
    [SerializeField] private GameObject _continueButton;
    [SerializeField] private GameObject _skillTreeButton;
    [SerializeField] private GameObject _loadOrNewGameButton;

    private void Start()
    {
        // 실제 저장 시스템과 연결할 부분
        bool hasSaveData = CheckSaveData();

        SetSaveDataUI(hasSaveData);
    }

    private bool CheckSaveData()
    {
        // 나중에 실제 저장 데이터 확인 로직으로 교체
        return false;
    }
    
    /// <summary>
    /// 저장 데이터가 있을 때 표시할 UI
    /// </summary>
    private void SetSaveDataUI(bool hasSaveData)
    {
        _continueButton.SetActive(hasSaveData);
        _skillTreeButton.SetActive(hasSaveData);
        _loadOrNewGameButton.SetActive(hasSaveData);
    }
}