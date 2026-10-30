using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 저장 슬롯의 데이터 유무에 따라 버튼 표시와 동작을 관리
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    #region 인스펙터
    [Header("슬롯 설정")]
    [SerializeField, Range(1, 3)] private int _slotIndex = 1;

    [Header("버튼 UI")]
    [SerializeField] private Button _slotButton;
    [SerializeField] private TMP_Text _buttonText;
    [SerializeField] private Image _buttonIcon;

    [Header("버튼 아이콘")]
    [SerializeField] private Sprite _loadGameIcon;
    [SerializeField] private Sprite _newGameIcon;

    [Header("삭제 UI")]
    [SerializeField] private Button _deleteButton;
    [SerializeField] private SaveSlotDeleteConfirmUI _deleteConfirmUI;

    [Header("저장 상태 텍스트")]
    [SerializeField] private TMP_Text _slotStatusText;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        RefreshSlotUI();

        RegisterButtonListeners();

        _deleteConfirmUI.OnSlotDeleted += HandleSlotDeleted;
    }

    private void OnDisable()
    {
        RemoveButtonListeners();

        _deleteConfirmUI.OnSlotDeleted -= HandleSlotDeleted;
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 저장 데이터 유무에 따라 버튼 문구와 동작을 갱신
    /// </summary>
    public void RefreshSlotUI()
    {
        bool hasSaveData = SaveManager.Instance.HasSaveData(_slotIndex);

        // 저장 상태 표시
        _slotStatusText.text = hasSaveData ? $"세이브 {_slotIndex}" : "비어 있음";

        // 실행 버튼 표시
        _buttonText.text = hasSaveData ? "불러오기" : "새 게임";

        // 버튼 아이콘 변경
        _buttonIcon.sprite = hasSaveData ? _loadGameIcon : _newGameIcon;

        // 삭제 버튼은 저장 데이터가 있을 때만 표시
        _deleteButton.gameObject.SetActive(hasSaveData);
    }
    #endregion

    #region 커스텀 (private)
    private void RegisterButtonListeners()
    {
        if (_slotButton != null)
        {
            _slotButton.onClick.RemoveListener(OnSlotButtonClicked);
            _slotButton.onClick.AddListener(OnSlotButtonClicked);
        }

        if (_deleteButton != null)
        {
            _deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
            _deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
    }

    /// <summary>
    /// 버튼 이벤트 해제
    /// </summary>
    private void RemoveButtonListeners()
    {
        if (_slotButton != null)
        {
            _slotButton.onClick.RemoveListener(OnSlotButtonClicked);
        }

        if (_deleteButton != null)
        {
            _deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
        }
    }

    private void OnSlotButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return;
        }

        bool hasSaveData = SaveManager.Instance.HasSaveData(_slotIndex);

        // 저장 데이터가 있으면 해당 슬롯을 불러옴.
        if (hasSaveData)
        {
            LoadGame();
        }
        // 없으면 새 게임을 시작
        else
        {
            StartNewGame();
        }
    }

    private void LoadGame()
    {
        if (!SaveManager.Instance.SetCurrentSlotIndex(_slotIndex))
        {
            return;
        }

        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.LoadGame, _slotIndex);

        OpenGameScene();
    }

    private void StartNewGame()
    {
        if (SaveManager.Instance.HasSaveData(_slotIndex))
        {
            Debug.LogWarning($"저장 {_slotIndex}에 데이터가 생겼으므로 불러오기 선택");

            RefreshSlotUI();
            return;
        }

        if (!SaveManager.Instance.SetCurrentSlotIndex(_slotIndex))
        {
            return;
        }

        GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.NewGame, _slotIndex);

        OpenGameScene();
    }

    private void OpenGameScene()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager를 찾을 수 없음.");
            return;
        }

        SceneTransitionManager.Instance.LoadGameScene();
    }

    /// <summary>
    /// 삭제 버튼 클릭 시 확인 패널 표시
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        if (!SaveManager.Instance.HasSaveData(_slotIndex))
        {
            RefreshSlotUI();
            return;
        }

        _deleteConfirmUI.Open(_slotIndex);
    }

    private void HandleSlotDeleted(int deletedSlotIndex)
    {
        if (_slotIndex != deletedSlotIndex)
        {
            return;
        }

        RefreshSlotUI();
    }
    #endregion
}
