using System.Collections;
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
    #region 속성
    public SpecialCoinType CoinType => _coinType;
    #endregion

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
    [SerializeField] private float _dropDuration = 0.4f;
    [SerializeField] private float _dropHeightY = 1.5f;
    [SerializeField] private float _minDistance = 1.5f;
    [SerializeField] private float _maxDistance = 3.0f;
    [SerializeField] private float _idleDuration = 2.0f;
    [SerializeField] private float _idleRotateSpeed = 180f;
    [SerializeField] private float _groundY = 2.1f;

    [Header("흡수 연출 설정")]
    [SerializeField] private float _pickUpTime = 0.25f;
    [SerializeField] private float _absorbRotateSpeed = 720f;
    [SerializeField] private Vector2 _absorbViewportPos = new Vector2(0.1f, -0.3f);
    [SerializeField] private float _absorbDepthFromCam = 2.0f;
    [SerializeField] private float _absorbArcHeight = 0.35f;
    #endregion

    #region 내부 변수
    private Camera _mainCamera;
    private Vector3 _cachedAbsorbTarget;
    private Coroutine _coinCoroutine;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared += HandleStageCleared;
        }
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
        }

        if (_coinCoroutine != null)
        {
            StopCoroutine(_coinCoroutine);
            _coinCoroutine = null;
        }
    }
    #endregion

    #region 커스텀 함수 (Public)
    /// <summary>
    /// 스폰 좌표를 명시적으로 전달받아 드롭-대기-흡수 연출을 시작합니다.
    /// </summary>
    public void Init(Vector3 spawnPos)
    {
        // 이미 스테이지가 클리어된 상태라면 즉시 풀로 반환
        if (StageManager.Instance != null && !StageManager.Instance.IsStageActive)
        {
            ReturnToPool();
            return;
        }

        transform.position = spawnPos;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            Vector3 viewportPoint = new Vector3(_absorbViewportPos.x, _absorbViewportPos.y, _absorbDepthFromCam);
            _cachedAbsorbTarget = _mainCamera.ViewportToWorldPoint(viewportPoint);
        }

        if (_coinCoroutine != null)
        {
            StopCoroutine(_coinCoroutine);
        }

        _coinCoroutine = StartCoroutine(SpecialCoinRoutine(spawnPos));
    }
    #endregion

    #region 커스텀 함수(private)
    private void HandleStageCleared(int stageNumber)
    {
        if (_coinCoroutine != null)
        {
            StopCoroutine(_coinCoroutine);
            _coinCoroutine = null;
        }

        // 흡수 연출 및 효과 발동 없이 즉시 풀로 반환
        ReturnToPool();
    }

    private IEnumerator SpecialCoinRoutine(Vector3 startPos)
    {
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 randomDir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        float randomDistance = Random.Range(_minDistance, _maxDistance);

        Vector3 targetPos = startPos + new Vector3(randomDir.x, 0f, randomDir.y) * randomDistance;
        targetPos.y = _groundY;

        // 포물선 드롭
        float elapsed = 0f;
        while (elapsed < _dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _dropDuration);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * _dropHeightY;
            transform.position = currentPos;

            transform.Rotate(Vector3.up * (_idleRotateSpeed * 2f * Time.deltaTime), Space.World);
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

        // 흡수
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
            transform.Rotate(Vector3.up * (_absorbRotateSpeed * Time.deltaTime), Space.World);

            yield return null;
        }

        // 특수 효과 발동 및 풀로 반환
        AudioManager.Instance.PlaySFX(EAudioType.CoinGet);
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

        AudioManager.Instance.PlaySFXDelayed(EAudioType.CoinGet, 0.21f, 1.5f);

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

