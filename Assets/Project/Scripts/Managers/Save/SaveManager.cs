using System;
using UnityEngine;

/// <summary>
/// 게임 데이터의 저장 및 불러오기를 관리합니다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    #region 싱글톤

    public static SaveManager Instance { get; private set; }

    #endregion

    #region 이벤트

    public event Action OnGameSaved;
    public event Action<GameSaveData> OnGameLoaded;

    #endregion

    #region 이벤트 함수

    /// <summary>
    /// SaveManager 싱글톤을 등록합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// SaveManager가 파괴될 때 싱글톤 참조를 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 전달받은 게임 데이터를 저장합니다.
    /// </summary>
    public void SaveGame(GameSaveData saveData)
    {
        // TODO: 게임 데이터 저장
    }

    /// <summary>
    /// 저장된 게임 데이터를 불러옵니다.
    /// </summary>
    public GameSaveData LoadGame()
    {
        // TODO: 저장 데이터 불러오기
        return null;
    }

    /// <summary>
    /// 저장된 게임 데이터가 존재하는지 확인합니다.
    /// </summary>
    public bool HasSaveData()
    {
        // TODO: 저장 데이터 존재 여부 확인
        return false;
    }

    /// <summary>
    /// 저장된 게임 데이터를 삭제합니다.
    /// </summary>
    public void DeleteSaveData()
    {
        // TODO: 저장 데이터 삭제
    }

    #endregion
}

