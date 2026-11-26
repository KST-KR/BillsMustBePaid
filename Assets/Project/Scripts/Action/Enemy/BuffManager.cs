using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : Singleton<BuffManager>
{
    #region 속성
    public float JackpotChanceMultiplier { get; private set; } = 1f;
    public float CoinGainMultiplier { get; private set; } = 1f;

    public bool IsJackpotBuffActive => _jackpotRoutine != null;
    public bool IsDoubleCoinBuffActive => _doubleCoinRoutine != null;

    public float JackpotRemainingTime { get; private set; }
    public float DoubleCoinRemainingTime { get; private set; }

    public float JackpotDuration { get; private set; }
    public float DoubleCoinDuration { get; private set; }

    public float JackpotProgress => (JackpotDuration > 0f) ? Mathf.Clamp01(JackpotRemainingTime / JackpotDuration) : 0f;
    public float DoubleCoinProgress => (DoubleCoinDuration > 0f) ? Mathf.Clamp01(DoubleCoinRemainingTime / DoubleCoinDuration) : 0f;
    #endregion

    #region 이벤트
    public event Action<bool> OnJackpotBuffStateChanged;
    public event Action<bool> OnDoubleCoinBuffStateChanged;
    #endregion

    #region 내부 변수
    private Coroutine _jackpotRoutine;
    private Coroutine _doubleCoinRoutine;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        ResetAllBuffs();
    }

    private void Start()
    {
        if (!IsActiveInstance)
        {
            return;
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared += HandleStageCleared;
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
        }

        ResetAllBuffs();
    }
    #endregion

    #region 커스텀 함수(public)
    /// <summary>
    /// 잭팟 확률 버프 발동 (중복 흭득 시 남은 시간 갱신)
    /// </summary>
    public void ActivateJackpotBuff(float duration, float multiplier)
    {
        if (_jackpotRoutine != null)
        {
            StopCoroutine(_jackpotRoutine);
        }

        JackpotChanceMultiplier = multiplier;
        JackpotRemainingTime = duration;
        JackpotDuration = duration;

        _jackpotRoutine = StartCoroutine(JackpotBuffRoutine());

        OnJackpotBuffStateChanged?.Invoke(true);

        Debug.Log($"BuffManager : 잭팟 확률 {multiplier}배 버프 시작! ({duration}초)");
    }

    /// <summary>
    /// 코인 획득량 2배 버프 발동 (중복 획득 시 남은 시간 갱신)
    /// </summary>
    public void ActivateDoubleCoinBuff(float duration, float multiplier)
    {
        if (_doubleCoinRoutine != null)
        {
            StopCoroutine(_doubleCoinRoutine);
        }

        CoinGainMultiplier = multiplier;
        DoubleCoinRemainingTime = duration;
        DoubleCoinDuration = duration;

        _doubleCoinRoutine = StartCoroutine(DoubleCoinBuffRoutine());

        OnDoubleCoinBuffStateChanged?.Invoke(true);

        Debug.Log($"[BuffManager] 코인 획득량 {multiplier}배 버프 시작! ({duration}초)");
    }

    /// <summary>
    /// 스테이지 종료 또는 초기화 시 모든 버프 강제 해제
    /// </summary>
    public void ResetAllBuffs()
    {
        if (_jackpotRoutine != null)
        {
            StopCoroutine(_jackpotRoutine);
            _jackpotRoutine = null;
        }

        if (_doubleCoinRoutine != null)
        {
            StopCoroutine(_doubleCoinRoutine);
            _doubleCoinRoutine = null;
        }

        JackpotChanceMultiplier = 1f;
        CoinGainMultiplier = 1f;
        JackpotRemainingTime = 0f;
        DoubleCoinRemainingTime = 0f;

        OnJackpotBuffStateChanged?.Invoke(false);
        OnDoubleCoinBuffStateChanged?.Invoke(false);
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator JackpotBuffRoutine()
    {
        while (JackpotRemainingTime > 0f)
        {
            JackpotRemainingTime -= Time.deltaTime;
            yield return null;
        }

        JackpotRemainingTime = 0f;
        JackpotChanceMultiplier = 1f;
        _jackpotRoutine = null;
        OnJackpotBuffStateChanged?.Invoke(false);

        Debug.Log("[BuffManager] 잭팟 확률 버프 종료");
    }

    private IEnumerator DoubleCoinBuffRoutine()
    {
        while (DoubleCoinRemainingTime > 0f)
        {
            DoubleCoinRemainingTime -= Time.deltaTime;
            yield return null;
        }

        DoubleCoinRemainingTime = 0f;
        CoinGainMultiplier = 1f;
        _doubleCoinRoutine = null;
        OnDoubleCoinBuffStateChanged?.Invoke(false);

        Debug.Log("[BuffManager] 코인 획득량 버프 종료");
    }

    private void HandleStageCleared(int stageNumber)
    {
        ResetAllBuffs();
    }
    #endregion
}
