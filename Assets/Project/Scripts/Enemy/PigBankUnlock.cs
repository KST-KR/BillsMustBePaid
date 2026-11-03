using System;
using UnityEngine;

[Serializable]
public struct UnlockInfo
{
    [Tooltip("해금에 필요한 파괴해야하는 돼지 저금통 수")]
    public int unlockCount;

    [Tooltip("해금 되는 돼지 저금통")]
    public PigBankDataBaseSO _pigData;
}

public class PigBankUnlock : MonoBehaviour
{
    #region 싱글톤
    public static PigBankUnlock Instance { get; private set; }
    #endregion

    #region 인스펙터
    [Header("해금 데이터 설정 (인스펙터에서 수정 가능)")]
    [SerializeField] private UnlockInfo[] _unlockTable;
    #endregion

    #region 내부 변수
    private int _unlockLevel = 1;
    private int _totalCrash = 0;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
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
                _unlockLevel++;
                Debug.LogError($" ← unlock →  : 돼지 저금통 해금 해금 레벨: {_unlockLevel}");              
            }
        }
    }

    /// <summary>
    /// 해금 레벨에 해당하는 보상을 반환
    /// </summary>
    public PigBankDataBaseSO GetUnlockLevelBonus()
    {
        int currentIndex = _unlockLevel - 1;
        if (currentIndex >= 0 && currentIndex < _unlockTable.Length)
        {
            return _unlockTable[currentIndex]._pigData;
        }
        return null; // 예외 처리
    }

    /// <summary>
    /// 게임 오버(파산) 시 파괴 레벨 시스템을 초기화
    /// </summary>
    public void ResetUnlockSystem()
    {
        _unlockLevel = 1;
        _totalCrash = 0;
        Debug.Log("StageReward : 파산/게임 오버로 인해 해금 레벨이 초기화되었습니다.");
    }
    #endregion


}
