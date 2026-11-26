using System;
using UnityEngine;

/// <summary>
/// 스테이지 내 공격 성공률을 관리
/// 돼지 저금통 적중은 성공, 책상 적중은 실패로 기록
/// </summary>
public class PlayerAccuracy : MonoBehaviour
{
    #region 내부 변수
    private int _totalAttacks;
    private int _winCount;
    private int _loseCount;

    private StageManager _stageManager;
    #endregion

    #region 프로퍼티
    public int TotalAttacks => _totalAttacks;
    public int WinCount => _winCount;
    public int LoseCount => _loseCount;

    public float Accuracy
    {
        get
        {
            if (_totalAttacks == 0)
            {
                return 100f;
            }

            return (float)_winCount / _totalAttacks * 100f;
        }
    }
    #endregion

    #region 이벤트
    public event Action<float> OnAccuracyChanged;
    #endregion

    #region 이벤트 함수
    private void Start()
    {
        _stageManager = StageManager.Instance;

        if (_stageManager == null)
        {
            Debug.LogWarning("StageManager를 찾을 수 없음.", this);
            return;
        }

        _stageManager.OnStageStarted += HandleStageStarted;
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.OnStageStarted -= HandleStageStarted;
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 돼지 저금통 적중을 성공으로 기록
    /// </summary>
    public void RecordWin()
    {
        _totalAttacks++;
        _winCount++;

        OnAccuracyChanged?.Invoke(Accuracy);
    }

    /// <summary>
    /// 책상 적중을 실패로 기록
    /// </summary>
    public void RecordLose()
    {
        _totalAttacks++;
        _loseCount++;

        OnAccuracyChanged?.Invoke(Accuracy);
    }

    /// <summary>
    /// 스테이지 시작 시 공격 기록을 초기화
    /// </summary>
    public void ResetAccuracy()
    {
        _totalAttacks = 0;
        _winCount = 0;
        _loseCount = 0;

        OnAccuracyChanged?.Invoke(Accuracy);
    }
    #endregion

    #region 커스텀 (private)
    private void HandleStageStarted(int stageNumber)
    {
        ResetAccuracy();

        Debug.Log($"스테이지 {stageNumber} 시작 - 정확도 초기화: {Accuracy:F1}%");
    }
    #endregion
}
