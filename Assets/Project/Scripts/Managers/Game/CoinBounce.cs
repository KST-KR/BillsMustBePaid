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
    #endregion

    #region 내부 변수
    private Coroutine _bounceCoroutine;
    private CoinItem _coinItem;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _coinItem = GetComponent<CoinItem>();
    }

    private void OnEnable()
    {
        _bounceCoroutine = StartCoroutine(BounceRoutine());
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator BounceRoutine()
    {
        Vector3 startPos = transform.position;

        // 랜덤 방향과 거리를 이용해 최종 안착할 목표 지점 계산
        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        float randomDistance = UnityEngine.Random.Range(_maxDistance, _maxDistance);
        Vector3 targetPos = startPos + new Vector3(randomDir.x, 0, randomDir.y) * randomDistance;

        // 회전 을 위한 랜덤 회전 속도 설전
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(_torqueMin, _torqueMax),
            UnityEngine.Random.Range(_torqueMin, _torqueMax),
            UnityEngine.Random.Range(_torqueMin, _torqueMax)
            ) * 100f;

        float duration = 0.4f;
        float elapsedTime = 0f;

        while(elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // xz 평면 선형 보간
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // y 축 포물선 높이 추가
            currentPos.y += Mathf.Sin(t * Mathf.PI) * _heightY;
            transform.position = currentPos;

            // 회전적용
            transform.Rotate(randomTorque * Time.deltaTime);

            yield return null;
        }

        transform.position = targetPos;

        float waitElapsedTime = 0f;
        while(waitElapsedTime < _removeTime)
        {
            waitElapsedTime += Time.deltaTime;
            yield return null;
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
