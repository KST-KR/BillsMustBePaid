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

    protected override void OnSingletonDestroy()
    {
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

        _jackpotRoutine = StartCoroutine(JackpotBuffRoutine(duration, multiplier));
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

        _doubleCoinRoutine = StartCoroutine(DoubleCoinBuffRoutine(duration, multiplier));
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
    private IEnumerator JackpotBuffRoutine(float duration, float multiplier)
    {
        JackpotChanceMultiplier = multiplier;
        JackpotRemainingTime = duration;
        OnJackpotBuffStateChanged?.Invoke(true);

        Debug.Log($"BuffManager : 잭팟 확률 {multiplier}배 버프 시작! ({duration}초)");

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

    private IEnumerator DoubleCoinBuffRoutine(float duration, float multiplier)
    {
        CoinGainMultiplier = multiplier;
        DoubleCoinRemainingTime = duration;
        OnDoubleCoinBuffStateChanged?.Invoke(true);

        Debug.Log($"[BuffManager] 코인 획득량 {multiplier}배 버프 시작! ({duration}초)");

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
    #endregion
}
