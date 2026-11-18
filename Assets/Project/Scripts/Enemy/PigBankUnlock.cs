using System;
using UnityEngine;

[Serializable]
public struct UnlockInfo
{
    [Tooltip("해금에 필요한 파괴해야하는 돼지 저금통 수")]
    public int unlockCount;

    [Tooltip("해금 되는 돼지 저금통")]
    public PigBankDataBaseSO _pigData;

    [Tooltip("UI 표시용 돼지 이름 및 아이콘")]
    public string _pigName;
    public Sprite pigIcon;
}

public class PigBankUnlock : Singleton<PigBankUnlock>
{
    #region 속성
    public int UnlockLevel => _unlockLevel;
    public int TotalCrash => _totalCrash;
    #endregion

    #region 이벤트
    public event Action<int, int> OnUnlockProgressChanged;
    public event Action<UnlockInfo> OnPigBankUnlocked;
    #endregion

    #region 인스펙터
    [Header("해금 데이터 설정 (인스펙터에서 수정 가능)")]
    [SerializeField] private UnlockInfo[] _unlockTable;
    #endregion

    #region 내부 변수
    private int _unlockLevel = 1;
    private int _totalCrash = 0;
    #endregion

    #region 커스텀 함수(public)
    /// <summary>
    /// 돼지 저금통이 파괴될 때 호출되어 누적 횟수와 해금 관리
    /// </summary>
    public void UnlockPigBank()
    {
        _totalCrash++;
        Debug.Log($"StageReward : 돼지 파괴 누적 횟수: {_totalCrash}");

        int currentIndex = _unlockLevel - 1;

        // 배열 범위를 벗어나지 않는지 확인
        if (currentIndex < _unlockTable.Length)
        {
            // 현재 레벨에 설정된 목표 횟수에 도달했는지 확인
            if (_totalCrash >= _unlockTable[currentIndex].unlockCount)
            {
                UnlockInfo unlockedInfo = _unlockTable[currentIndex];

                _unlockLevel++;
                Debug.Log($" ← unlock →  : 돼지 저금통 해금 해금 레벨: {_unlockLevel}");

                OnPigBankUnlocked?.Invoke(unlockedInfo);
            }
        }

        OnUnlockProgressChanged?.Invoke(_totalCrash, _unlockLevel);
    }

    /// <summary>
    /// 해금 레벨에 해당하는 보상을 반환
    /// </summary>
    public PigBankDataBaseSO GetUnlockLevel()
    {
        int currentIndex = _unlockLevel - 1;
        if (currentIndex >= 0 && currentIndex < _unlockTable.Length)
        {
            return _unlockTable[currentIndex]._pigData;
        }
        return null;
    }

    /// <summary>
    /// 게임 오버(파산) 시 파괴 레벨 시스템을 초기화
    /// </summary>
    public void ResetUnlockSystem()
    {
        _unlockLevel = 1;
        _totalCrash = 0;
        OnUnlockProgressChanged?.Invoke(_totalCrash, _unlockLevel);
        Debug.Log("StageReward : 파산/게임 오버로 인해 해금 레벨이 초기화되었습니다.");
    }

    /// <summary>
    /// 다음에 해금될 돼지 저금통 정보와 남은 파괴 횟수를 반환
    /// 모든 돼지 저금통을 다 해금했으면 false 반환
    /// </summary>
    public bool TryGetNextUnlockInfo(out UnlockInfo nextInfo, out int remainingCount)
    {
        int nextIndex = _unlockLevel - 1;

        if(_unlockTable != null && nextIndex >= 0 && nextIndex < _unlockTable.Length)
        {
            nextInfo = _unlockTable[nextIndex];
            remainingCount = Mathf.Max(0, nextInfo.unlockCount - _totalCrash);
            return true;
        }

        nextInfo = default;
        remainingCount = 0;
        return false;
    }

    /// <summary>
    /// 해금 데이터 내보내기, 복원하기
    /// </summary>
    public void ExportSaveData(RunPiggyBankSaveData data)
    {
        if(data == null)
        {
            return;
        }

        data.unlockLevel = _unlockLevel;
        data.unlockTotalCrash = _totalCrash;
    }

    public void ImportSaveData(RunPiggyBankSaveData data)
    {
        if(data == null)
        {
            ResetUnlockSystem();
            return;
        }

        _unlockLevel = Mathf.Max(1, data.unlockLevel);
        _totalCrash = Mathf.Max(0, data.unlockTotalCrash);
        OnUnlockProgressChanged?.Invoke(_totalCrash, _unlockLevel);
    }
    #endregion
}
