using System.Collections;
using UnityEngine;

public class CoinBounce : MonoBehaviour
{
    #region 인스펙터
    [Header("코인 날아가는 설정")]
    [SerializeField] private float _dropDuration = 0.4f;
    [SerializeField] private float _heightY = 1.5f;
    [SerializeField] private float _minDistance = 2.0f;
    [SerializeField] private float _maxDistance = 4.0f;
    [SerializeField] private float _groundY = 1.6f;
    [SerializeField] private float _upModifier = 1.0f;
    [SerializeField] private float _torqueMin = 1.0f;
    [SerializeField] private float _torqueMax = 3.0f;

    [Header("테이블 위에 있다가 정리 되는 시간")]
    [SerializeField] private float _removeTime = 2.0f;

    [Header("코인 흡수 연출 설정")]
    [SerializeField] private float _pickUpTime = 0.25f;
    [SerializeField] private float _absorbRotateSpeed = 720f;
    [SerializeField] private Vector2 _absorbViewportPos = new Vector2(0.1f, -0.3f);
    [SerializeField] private float _absorbDepthFromCam = 2.0f;
    [SerializeField] private float _absorbArcHeight = 0.35f;
    #endregion

    #region 내부 변수
    private Coroutine _bounceCoroutine;
    private CoinItem _coinItem;
    private Camera _mainCamera;
    private Vector3 _initialScale;
    private Vector3 _cachedAbsorbTarget;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _coinItem = GetComponent<CoinItem>();
        _mainCamera = Camera.main;
        _initialScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }
    }
    #endregion

    #region 커스텀 함수 (Public)
    /// <summary>
    /// 스폰 좌표를 명시적으로 전달받아 드롭-대기-흡수 연출을 시작합니다.
    /// </summary>
    public void Init(Vector3 spawnPos)
    {
        transform.position = spawnPos;
        transform.localScale = _initialScale;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            Vector3 viewportPoint = new Vector3(_absorbViewportPos.x, _absorbViewportPos.y, _absorbDepthFromCam);
            _cachedAbsorbTarget = _mainCamera.ViewportToWorldPoint(viewportPoint);
        }

        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
        }

        _bounceCoroutine = StartCoroutine(BounceRoutine(spawnPos));
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator BounceRoutine(Vector3 startPos)
    {
        // 영벡터 방지를 위해 각도 기반으로 랜덤 방향 계산
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 randomDir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        float randomDistance = Random.Range(_minDistance, _maxDistance);

        Vector3 targetPos = startPos + new Vector3(randomDir.x, 0f, randomDir.y) * randomDistance;
        targetPos.y = _groundY;

        Vector3 randomTorque = new Vector3(
            Random.Range(_torqueMin, _torqueMax),
            Random.Range(_torqueMin, _torqueMax),
            Random.Range(_torqueMin, _torqueMax)
        ) * 150f;

        float elapsedTime = 0f;
        Quaternion targetRot = Quaternion.identity;

        // 포물선 드롭
        while (elapsedTime < _dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float rawT = elapsedTime / _dropDuration;
            float t = Mathf.SmoothStep(0f, 1f, rawT);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * _heightY;
            transform.position = currentPos;

            if (rawT > 0.7f)
            {
                float landingT = (rawT - 0.7f) / 0.3f;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, landingT);
            }
            else
            {
                transform.Rotate(randomTorque * (1f - rawT) * Time.deltaTime);
            }

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        // 테이블 위 대기
        float waitElapsedTime = 0f;
        while (waitElapsedTime < _removeTime)
        {
            waitElapsedTime += Time.deltaTime;
            yield return null;
        }

        // UI 방향으로 흡수
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
            transform.localScale = Vector3.Lerp(_initialScale, _initialScale * 0.25f, easeT);

            yield return null;
        }

        // 풀로 반환
        if (StageRewardManager.Instance != null && _coinItem != null)
        {
            StageRewardManager.Instance.AddEarnedCoinCount(_coinItem._coinType, 1);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (CoinObjectPoolManager.Instance != null && _coinItem != null)
        {
            CoinObjectPoolManager.Instance.ReturnCoin(_coinItem._coinType, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

}
