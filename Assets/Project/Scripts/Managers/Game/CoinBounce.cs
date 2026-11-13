using System.Collections;
using UnityEngine;

public class CoinBounce : MonoBehaviour
{
    #region 인스펙터
    [Header("코인 날아가는 설정")]
    [SerializeField] private float _heightY = 1.5f;
    [SerializeField] private float _minDistance = 2.0f;
    [SerializeField] private float _maxDistance = 4.0f;
    [SerializeField] private float _upModifier = 1.0f;
    [SerializeField] private float _torqueMin = 1.0f;
    [SerializeField] private float _torqueMax = 3.0f;

    [Header("테이블 위에 있다가 정리 되는 시간")]
    [SerializeField] private float _removeTime = 2.0f;

    [Header("코인 흡수 연출 설정")]
    [SerializeField] private float _pickUpTime = 0.25f;
    [SerializeField] private Vector2 _absorbViewportPos = new Vector2(0.1f, -0.3f);
    [SerializeField] private float _absorbDepthFromCam = 2.0f;
    [SerializeField] private float _absorbArcHeight = 0.35f;
    #endregion

    #region 내부 변수
    private Coroutine _bounceCoroutine;
    private CoinItem _coinItem;
    private Vector3 _scale;
    private Vector3 _cachedAbsorbTarget;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _coinItem = GetComponent<CoinItem>();
        _scale = transform.localScale;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            Vector3 viewportPoint = new Vector3(_absorbViewportPos.x, _absorbViewportPos.y, _absorbDepthFromCam);
            _cachedAbsorbTarget = Camera.main.ViewportToWorldPoint(viewportPoint);
        }
    }

    private void OnEnable()
    {
        transform.localScale = _scale;
        _bounceCoroutine = StartCoroutine(BounceRoutine());
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator BounceRoutine()
    {
        Vector3 startPos = transform.position;

        // 랜덤 방향과 거리를 이용해 최종 안착할 목표 지점 계산
        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        float randomDistance = UnityEngine.Random.Range(_minDistance, _maxDistance);
        Vector3 targetPos = startPos + new Vector3(randomDir.x, 0, randomDir.y) * randomDistance;

        targetPos.y = 1.6f;

        // 회전 을 위한 랜덤 회전 속도 설전
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(_torqueMin, _torqueMax),
            UnityEngine.Random.Range(_torqueMin, _torqueMax),
            UnityEngine.Random.Range(_torqueMin, _torqueMax)
            ) * 150f;

        float duration = 0.4f;
        float elapsedTime = 0f;

        // 착지 직전의 회전을 부드럽게 처리하기 위한 시작 회전값 저장
        Quaternion startRot = transform.rotation;
        // 바닥에 누울 목표 회전값 (평평한 상태)
        Quaternion targetRot = Quaternion.identity;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float rawT = elapsedTime / duration;

            float t = Mathf.SmoothStep(0f, 1f, rawT);

            // xz 평면 선형 보간
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // y 축 포물선 높이 추가
            currentPos.y += Mathf.Sin(t * Mathf.PI) * _heightY;
            transform.position = currentPos;

            if (rawT > 0.7f)
            {
                // 0.7 ~ 1.0 구간을 0 ~ 1 로 환산
                float landingT = (rawT - 0.7f) / 0.3f;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, landingT);
            }
            else
            {
                // 전반부에는 회전 적용
                transform.Rotate(randomTorque * (1f - rawT) * Time.deltaTime);
            }

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        float waitElapsedTime = 0f;
        while(waitElapsedTime < _removeTime)
        {
            waitElapsedTime += Time.deltaTime;
            yield return null;
        }
          
        float absorbElapsed = 0f;
        Vector3 absorbStartPos = transform.position;

        while(absorbElapsed < _pickUpTime)
        {
            absorbElapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(absorbElapsed / _pickUpTime);

            // Ease-In 적용: 처음엔 살짝 끌려오다가 후반에 빠르게 빨려 들어감
            float easeT = rawT * rawT;

            Vector3 currentPos = Vector3.Lerp(absorbStartPos, _cachedAbsorbTarget, easeT);
            currentPos.y += Mathf.Sin(rawT * Mathf.PI) * _absorbArcHeight;

            transform.position = currentPos;
            transform.Rotate(Vector3.up * 720f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(_scale, _scale * 0.25f, easeT);

            yield return null;
        }

        if (StageRewardManager.Instance != null && _coinItem != null)
        {
            StageRewardManager.Instance.AddEarnedCoinCount(_coinItem._coinType, 1);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 강제로 꺼지거나 풀로 돌아갈 때 코루틴 확실하게 종료
        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }

        CoinObjectPoolManager.Instance.ReturnCoin(_coinItem._coinType, gameObject);     
    }
    #endregion

}
