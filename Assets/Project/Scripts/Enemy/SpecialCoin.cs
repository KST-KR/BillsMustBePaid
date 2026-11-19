using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region enum
public enum SpecialCoinType
{
    InstantCoin,      // 즉시 코인 200 지급
    JackpotChanceUp,  // 30초간 잭팟 확률 5배
    DoubleCoinGain    // 20초간 코인 획득량 2배
}
#endregion

public class SpecialCoin : MonoBehaviour
{
    #region 인스펙터
    [Header("특수 코인 설정")]
    [SerializeField] private SpecialCoinType _coinType;
    [SerializeField] private int _instantCoin = 500;
    [SerializeField] private bool _spawnVisualCoinsOnInstant = true;

    [Header("즉시 코인 지급 연출 드랍 위치")]
    [SerializeField] private Vector3 _instantCoinDropPos = new Vector3(0f, 3f, 0f);

    [Header("버프 수치 설정")]
    [SerializeField] private float _jackpotChanceUp = 5f;
    [SerializeField] private float _jackpotDuration = 30f;
    [SerializeField] private float _coinGainMultiplier = 2f;
    [SerializeField] private float _coinGainDuration = 20f;

    [Header("드롭 및 회전 설정")]
    [SerializeField] private float _dropHeightY = 1.5f;
    [SerializeField] private float _minDistance = 1.5f;
    [SerializeField] private float _maxDistance = 3.0f;
    [SerializeField] private float _idleDuration = 2.0f;
    [SerializeField] private float _idleRotateSpeed = 180f;
    [SerializeField] private float _groundY = 2.1f;

    [Header("흡수 연출 설정")]
    [SerializeField] private float _pickUpTime = 0.25f;
    [SerializeField] private Vector2 _absorbViewportPos = new Vector2(0.1f, -0.3f);
    [SerializeField] private float _absorbDepthFromCam = 2.0f;
    [SerializeField] private float _absorbArcHeight = 0.35f;
    #endregion

    #region 내부 변수
    private Vector3 _initialScale;
    private Vector3 _cachedAbsorbTarget;
    private Coroutine _coinCoroutine;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = _initialScale;

        if (Camera.main != null)
        {
            Vector3 viewportPoint = new Vector3(_absorbViewportPos.x, _absorbViewportPos.y, _absorbDepthFromCam);
            _cachedAbsorbTarget = Camera.main.ViewportToWorldPoint(viewportPoint);
        }

        _coinCoroutine = StartCoroutine(SpecialCoinRoutine());
    }

    private void OnDisable()
    {
        if (_coinCoroutine != null)
        {
            StopCoroutine(_coinCoroutine);
            _coinCoroutine = null;
        }
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator SpecialCoinRoutine()
    {
        Vector3 startPos = transform.position;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(_minDistance, _maxDistance);
        Vector3 targetPos = startPos + new Vector3(randomDir.x, 0f, randomDir.y) * randomDistance;
        targetPos.y = _groundY;

        float dropDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dropDuration);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * _dropHeightY;
            transform.position = currentPos;

            transform.Rotate(Vector3.up * _idleRotateSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;

        // 필드 위에서 제자리 회전하며 대기
        float waitElapsed = 0f;
        while (waitElapsed < _idleDuration)
        {
            waitElapsed += Time.deltaTime;
            transform.Rotate(Vector3.up * _idleRotateSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        float absorbElapsed = 0f;
        Vector3 absorbStartPos = transform.position;

        while (absorbElapsed < _pickUpTime)
        {
            absorbElapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(absorbElapsed / _pickUpTime);
            float easeT = rawT * rawT;

            Vector3 currentPos = Vector3.Lerp(absorbStartPos, _cachedAbsorbTarget, easeT);
            currentPos.y += Mathf.Sin(rawT * Mathf.PI) * _absorbArcHeight;

            transform.position = currentPos;
            transform.Rotate(Vector3.up * 720f * Time.deltaTime, Space.World);
            transform.localScale = Vector3.Lerp(_initialScale, _initialScale * 0.25f, easeT);

            yield return null;
        }

        // 특수 효과 발동 및 풀로 반환
        ActivateSpecialEffect();
        ReturnToPool();
    }

    private void ActivateSpecialEffect()
    {
        switch (_coinType)
        {
            case SpecialCoinType.InstantCoin:
                GrantInstantCoin();
                break;

            case SpecialCoinType.JackpotChanceUp:
                if (BuffManager.Instance != null)
                {
                    BuffManager.Instance.ActivateJackpotBuff(_jackpotDuration, _jackpotChanceUp);
                }
                Debug.Log($"{_jackpotDuration}초 동안 잭팟 확률 {_jackpotChanceUp}배 적용");
                break;

            case SpecialCoinType.DoubleCoinGain:
                if (BuffManager.Instance != null)
                {
                    BuffManager.Instance.ActivateDoubleCoinBuff(_coinGainDuration, _coinGainMultiplier);
                }
                Debug.Log($"{_coinGainDuration}초 동안 모든 코인 흭득량 {_coinGainMultiplier}배 적용");
                break;
        }
    }

    private void GrantInstantCoin()
    {
        int finalAmount = _instantCoin;

        // 코인 획득량 2배 버프가 활성화되어 있다면 즉시 지급 코인에도 적용
        if (BuffManager.Instance != null && BuffManager.Instance.IsDoubleCoinBuffActive)
        {
            finalAmount = Mathf.CeilToInt(finalAmount * BuffManager.Instance.CoinGainMultiplier);
        }

        if (StageRewardManager.Instance != null)
        {
            StageRewardManager.Instance.TryAddStageGold(finalAmount);
            Debug.Log($"SpecialCoin : 즉시 코인 {finalAmount} 지급 완료! (현재 누적: {StageRewardManager.Instance.StageRewardGold})");
        }

        // 코인 나오는 연출
        if (_spawnVisualCoinsOnInstant && CoinObjectPoolManager.Instance != null && CoinObjectPoolManager.Instance.IsEnabled)
        {
            int goldValue = CoinObjectPoolManager.Instance.GetCoinInfo(CoinObjectPoolManager.CoinType.GOLD)._value;
            int goldCount = (goldValue > 0) ? (finalAmount / goldValue) : 0;

            for (int i = 0; i < goldCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
                Vector3 spawnPos = _instantCoinDropPos + new Vector3(randomOffset.x, 0f, randomOffset.y);
                CoinObjectPoolManager.Instance.GetCoin(CoinObjectPoolManager.CoinType.GOLD, spawnPos, Quaternion.identity);
            }
        }
    }

    private void ReturnToPool()
    {
        if (_coinCoroutine != null)
        {
            StopCoroutine(_coinCoroutine);
            _coinCoroutine = null;
        }

        if (SpecialCoinPoolManager.Instance != null)
        {
            SpecialCoinPoolManager.Instance.ReturnCoin(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion
}

