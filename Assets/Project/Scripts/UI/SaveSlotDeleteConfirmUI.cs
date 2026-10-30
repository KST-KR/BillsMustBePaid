using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 저장 슬롯 삭제 확인 UI를 공통으로 관리
/// 현재 삭제 대상으로 선택된 슬롯 번호를 기억
/// 확인 버튼을 누르면 해당 슬롯의 데이터만 삭제
/// </summary>
public class SaveSlotDeleteConfirmUI : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private GameObject _deleteConfirmPanel;
    [SerializeField] private Button _confirmDeleteButton;
    [SerializeField] private Button _cancelDeleteButton;
    #endregion

    #region 내부 변수
    // 현재 삭제 대한으로 선택된 슬롯 번호
    private int _selectedSlotIndex = -1;
    #endregion

    #region 이벤트
    // 삭제가 성공하면 해당 슬롯 번호를 전달
    public event Action<int> OnSlotDeleted;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _deleteConfirmPanel.SetActive(false);
    }

    private void OnEnable()
    {
        _confirmDeleteButton.onClick.AddListener(OnConfirmDelete);
        _cancelDeleteButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        _confirmDeleteButton.onClick.RemoveListener(OnConfirmDelete);
        _cancelDeleteButton.onClick.RemoveListener(Close);
    }

    /// <summary>
    /// 삭제 확인 버튼을 눌렀을 때 실행
    /// </summary>
    private void OnConfirmDelete()
    {
        if (_selectedSlotIndex < 1)
        {
            Close();
            return;
        }

        int targetSlotIndex = _selectedSlotIndex;

        bool isDeleted = SaveManager.Instance.DeleteSaveData(targetSlotIndex);

        if (isDeleted)
        {
            OnSlotDeleted?.Invoke(targetSlotIndex);
        }

        Close();
    }

    /// <summary>
    /// 삭제를 취소하거나 확인 패널 닫기
    /// </summary>
    private void Close()
    {
        _selectedSlotIndex = -1;
        _deleteConfirmPanel.SetActive(false);
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 삭제할 슬롯을 지정하고 확인 패널 표시
    /// </summary>
    /// <param name="slotIndex"></param>
    public void Open(int slotIndex)
    {
        // 이미 저장된 데이터가 없는 슬롯은 삭제할 수 없음.
        if (!SaveManager.Instance.HasSaveData(slotIndex))
        {
            return;
        }

        _selectedSlotIndex = slotIndex;
        _deleteConfirmPanel.SetActive(true);
    }

    /// <summary>
    /// ESC 입력 등 외부 요청으로 확인 패널 닫기
    /// 패널이 열려 있으면 닫고 true를 반환
    /// </summary>
    /// <returns></returns>
    public bool TryClose()
    {
        if (!_deleteConfirmPanel.activeSelf)
        {
            return false;
        }

        Close();
        return true;
    }
    #endregion
}
