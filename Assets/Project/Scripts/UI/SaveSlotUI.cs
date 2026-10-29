using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 저장 슬롯의 데이터 유무에 따라 버튼 표시와 동작을 관리
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("슬롯 설정")]
    [SerializeField, Range(1, 3)] private int _slotIndex = 1;

    [Header("버튼 UI")]
    [SerializeField] private Button _slotButton;
    [SerializeField] private TMP_Text _buttonText;
    [SerializeField] private Image _buttonIcon;

    [Header("버튼 아이콘")]
    [SerializeField] private Sprite _loadGameIcon;
    [SerializeField] private Sprite _newGameIcon;

    #region 이벤트 함수
    private void OnEnable()
    {
        RefreshSlotUI();
    }

    private void OnDestroy()
    {
        if (_slotButton != null)
        {
            _slotButton.onClick.RemoveListener(OnSlotButtonClicked);
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 저장 데이터 유무에 따라 버튼 문구와 동작을 갱신
    /// </summary>
    public void RefreshSlotUI()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return;
        }

        if (_slotButton == null || _buttonText == null)
        {
            Debug.LogError($"저장 슬롯 {_slotIndex}의 버튼 UI가 연결 안됨", this);
            return;
        }

        bool hasSaveData = SaveManager.Instance.HasSaveData(_slotIndex);

        if (hasSaveData)
        {
            _buttonText.text = "불러오기";
            _buttonIcon.sprite = _loadGameIcon;
        }
        else
        {
            _buttonText.text = "새 게임";
            _buttonIcon.sprite = _newGameIcon;
        }

        // 기존 인스펙터 대신 코드에서 동작
        _slotButton.onClick.RemoveListener(OnSlotButtonClicked);
        _slotButton.onClick.AddListener(OnSlotButtonClicked);
    }
    #endregion

    #region 커스텀 (private)
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
        // 새 게임을 시작하기 직전에 다시 확인
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
    #endregion
}
