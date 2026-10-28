using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 데이터의 저장 및 불러오기를 관리
/// </summary>
public class SaveManager : MonoBehaviour
{
    #region 상수
    private const int MinSlotIndex = 1;
    private const int MaxSlotIndex = 3;
    #endregion

    #region 경로
    private const string SaveFileNameFormat = "save_slot_{0}.json";
    #endregion

    #region 싱글톤
    public static SaveManager Instance { get; private set; }
    #endregion

    #region 프로퍼티
    public int CurrentSlotIndex { get; private set; } = -1;
    #endregion

    #region 이벤트
    public event Action OnGameSaved;
    public event Action<SaveSlotData> OnGameLoaded;
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
        DontDestroyOnLoad(gameObject);
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

    #region 커스텀 (Private)
    private string GetSavePath(int slotIndex)
    {
        string fileName = string.Format(SaveFileNameFormat, slotIndex);

        return Path.Combine(Application.persistentDataPath, fileName);
    }

    /// <summary>
    /// 데이터가 누락된 경우 사용할 기본 객체를 준비
    /// </summary>
    /// <param name="saveDate"></param>
    private void EnsureValidData(SaveSlotData saveDate)
    {
        if (saveDate.playerData == null)
        {
            saveDate.playerData = new PlayerSaveData();
        }

        if (saveDate.playerData.permanentGrowth == null)
        {
            saveDate.playerData.permanentGrowth = new PermanentGrowthSaveData();
        }

        if (saveDate.playerData.permanentGrowth.purchasedItemIds == null)
        {
            saveDate.playerData.permanentGrowth.purchasedItemIds = new System.Collections.Generic.List<string>();
        }

        if (saveDate.runData == null)
        {
            saveDate.runData = new RunSaveData();
        }

        if (saveDate.runData.runGrowth == null)
        {
            saveDate.runData.runGrowth = new RunGrowthSaveData();
        }
    }

    /// <summary>
    /// 저장 슬롯의 최소, 최대 인덱스 값 반환
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= MinSlotIndex && slotIndex <= MaxSlotIndex;
    }

    /// <summary>
    /// 저장 파일을 읽고 데이터를 검증
    /// 불러오기 이벤트는 발생시키지 않음.
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    private SaveSlotData ReadSaveData(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return null;
        }

        string path = GetSavePath(slotIndex);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveSlotData saveData = JsonUtility.FromJson<SaveSlotData>(json);

            if (saveData == null || saveData.slotIndex != slotIndex)
            {
                return null;
            }

            EnsureValidData(saveData);

            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogError($"저장 데이터 읽기 실패: 슬롯 {slotIndex}, {exception.Message}");

            return null;
        }
    }
    #endregion

    #region 커스텀 메서드 (Public)

    /// <summary>
    /// 전달받은 게임 데이터를 저장합니다.
    /// </summary>
    public void SaveGame(SaveSlotData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("저장할 데이터가 없음.");
            return;
        }

        if (!IsValidSlotIndex(saveData.slotIndex))
        {
            Debug.LogError("잘못된 저장 슬롯 번호");
            return;
        }

        try
        {
            EnsureValidData(saveData);

            saveData.lastPlayedAt = DateTime.Now.ToString("o");

            string json = JsonUtility.ToJson(saveData, true);
            string path = GetSavePath(saveData.slotIndex);

            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(path, json);

            OnGameSaved?.Invoke();

            Debug.Log($"저장 완료: 슬롯 {saveData.slotIndex}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"게임 저장 실패: {exception.Message}");
        }
    }

    /// <summary>
    /// 지정한 슬롯을 읽고 해당 데이터의 복원을 알림.
    /// </summary>
    public SaveSlotData LoadGame(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError("잘못된 저장 슬롯 번호");
            return null;
        }

        // 내부 메서드를 이용해 데이터를 읽고 검증
        SaveSlotData saveData = ReadSaveData(slotIndex);

        if (saveData == null)
        {
            return null;
        }

        // 지정한 슬롯의 데이터만 외부에 알림
        OnGameLoaded?.Invoke(saveData);

        return saveData;
    }

    /// <summary>
    /// 지정한 슬롯에 저장 데이터가 존재하는지 확인
    /// </summary>
    public bool HasSaveData(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        return File.Exists(GetSavePath(slotIndex));
    }

    /// <summary>
    /// 세 개의 슬롯 중 하나라도 저장 데이터가 있는지 확인
    /// </summary>
    public bool HasSaveData()
    {
        for (int slotIndex = MinSlotIndex; slotIndex <= MaxSlotIndex; slotIndex++)
        {
            if (HasSaveData(slotIndex))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 저장된 게임 데이터를 삭제합니다.
    /// </summary>
    public void DeleteSaveData(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError("잘못된 저장 슬롯 번호입니다.");
            return;
        }

        string path = GetSavePath(slotIndex);

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"저장 데이터 삭제 완료: 슬롯 {slotIndex}");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"저장 데이터 삭제 실패: {exception.Message}");
        }
    }

    /// <summary>
    /// 진행 중인 런 중 가장 최근에 저장된 데이터를 불러오기
    /// </summary>
    public SaveSlotData LoadMostRecentlyPlayedGame()
    {
        SaveSlotData latestData = null;
        DateTime latestTime = DateTime.MinValue;

        for (int slotIndex = MinSlotIndex; slotIndex <= MaxSlotIndex; slotIndex++)
        {
            // 검색 중에는 불러오기 이벤트를 발생시키지 않음.
            SaveSlotData saveData = ReadSaveData(slotIndex);

            if (saveData == null)
            {
                continue;
            }

            // 진행 중인 런만 이어하기 대항으로 취급
            if (!saveData.runData.isRunActive)
            {
                continue;
            }

            if (!DateTime.TryParse(saveData.lastPlayedAt, out DateTime playedAt))
            {
                continue;
            }

            if (playedAt > latestTime)
            {
                latestTime = playedAt;
                latestData = saveData;
            }
        }

        // 검색이 끝난 뒤 최종 선택된 데이터만 알림.
        if (latestData != null)
        {
            OnGameLoaded?.Invoke(latestData);
        }

        return latestData;
    }

    /// <summary>
    /// 현재 사용할 저장 슬롯을 지정
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public bool SetCurrentSlotIndex(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"잘못된 저장 슬롯 번호: {slotIndex}");
            return false;
        }

        CurrentSlotIndex = slotIndex;
        return true;
    }

    #endregion
}