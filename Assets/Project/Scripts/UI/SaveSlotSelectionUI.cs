using UnityEngine;

/// <summary>
/// 저장 슬롯 1, 2, 3의 선택을 처리
/// 새 게임과 불러오기 동작을 구분
/// </summary>
public class SaveSlotSelectionUI : MonoBehaviour
{
    #region 열거형
    private enum ESlotSelectionMode
    {
        NewGame,
        LoadGame
    }
    #endregion

    #region 내부 변수
    private ESlotSelectionMode _selectionMode;
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 새 게임 버튼에서 호출
    /// </summary>
    public void BeginNewGame()
    {
        _selectionMode = ESlotSelectionMode.NewGame;
    }
    
    /// <summary>
    /// 불러오기 버튼에서 호출
    /// </summary>
    public void BeginLoadGame()
    {
        _selectionMode = ESlotSelectionMode.LoadGame;
    }

    public void OnSlot1ButtonClicked()
    {
        SelectSlot(1);
    }

    public void OnSlot2ButtonClicked()
    {
        SelectSlot(2);
    }

    public void OnSlot3ButtonClicked()
    {
        SelectSlot(3);
    }
    #endregion

    #region 커스텀 (private)
    private void SelectSlot(int slotIndex)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager를 찾을 수 없음.");
            return;
        }

        bool hasSaveData = SaveManager.Instance.HasSaveData(slotIndex);

        // 새 게임은 빈 슬롯에서만 시작
        // 기존 저장 데이터 덮어쓰기는 별도의 확인 절차 필요
        if (_selectionMode == ESlotSelectionMode.NewGame && hasSaveData)
        {
            Debug.LogWarning($"저장 {slotIndex}에는 이미 데이터가 있음." + "덮어쓰기 확인이 필요");
            return;
        }

        // 불러오기는 저장 데이터가 존재해야 함.
        if (_selectionMode == ESlotSelectionMode.LoadGame && !hasSaveData)
        {
            Debug.LogWarning($"저장 {slotIndex}에 불러올 데이터가 없음.");
            return;
        }

        SaveManager.Instance.SetCurrentSlotIndex(slotIndex);

        if (_selectionMode == ESlotSelectionMode.NewGame)
        {
            GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.NewGame, slotIndex);
        }
        else
        {
            GameLaunchData.SetLaunchData(GameLaunchData.ELaunchMode.LoadGame, slotIndex);
        }

        if (_selectionMode == ESlotSelectionMode.NewGame)
        {
            Debug.Log($"새 게임 슬롯 선택: {slotIndex}");
        }
        else
        {
            Debug.Log($"불러오기 슬롯 선택: {slotIndex}");
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager를 찾을 수 없음.");
            return;
        }

        SceneTransitionManager.Instance.LoadGameScene();
    }
    #endregion
}
