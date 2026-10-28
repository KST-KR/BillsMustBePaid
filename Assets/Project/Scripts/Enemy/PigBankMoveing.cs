using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PIG-AI] 돼지 저금통 움직임 관리
/// </summary>
public class PigBankMoveing : MonoBehaviour
{
    #region 인스펙터
    [Header("데이터 참조")]
    [SerializeField] private PigBankGameDataSO _pigData;

    [Header("점프 설정")]
    [SerializeField] private float _jumpHeight = 0.5f;
    [SerializeField] private float _jumpSpeed = 3f;

    [Header("겹침 설정")]
    [SerializeField] private float _radius = 3.5f;
    #endregion

    #region 내부 변수
    private float _startY;
    private Vector3 _moveDir;
    #endregion

    #region 이벤트
    void Start()
    {
        if (_pigData == null)
        {
            Debug.LogError("PigData가 없다 확인 필요");
            return;
        }

        _startY = transform.position.y;

        // 게임 시작시 무작위 방향 설정
        float randomAngle = Random.Range(0f, 360f);
        _moveDir = new Vector3(Mathf.Sin(randomAngle * Mathf.Deg2Rad), 0, Mathf.Cos(randomAngle * Mathf.Deg2Rad)).normalized;
    }


    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != EGameState.Playing)
        {
            return;
        }

        Moveing();
        PushOut();
    }

    /// <summary>
    /// 에디터에서 반구 범위를 그리기 위한 기즈모
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, _radius);
    }
    #endregion

    #region 커스텀 메서드 (private)
    private void Moveing()
    {
        if(_pigData == null)
        {
            Debug.LogError("PigData가 없다 확인 필요");
            return;
        }

        float currentSpeed = _pigData.MoveSpeed;

        // 진행 방향 앞에 보이지 않는 벽(Wall)이 있는지 레이캐스트로 검사
        if (Physics.Raycast(transform.position, _moveDir, out RaycastHit hit, 0.6f))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                // 벽에 부딪히면 반사각(Reflect)으로 자연스럽게 방향 전환
                _moveDir = Vector3.Reflect(_moveDir, hit.normal);
                _moveDir.y = 0;
                _moveDir.Normalize();
            }
        }

        Vector3 finalDir = _moveDir.normalized;

        // 전진
        transform.Translate(finalDir * currentSpeed * Time.deltaTime, Space.World);

        // 회전
        if (finalDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(finalDir.x, 0, finalDir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // Y축 점프 연출
        float newY = _startY + Mathf.Abs(Mathf.Sin(Time.time * _jumpSpeed)) * _jumpHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    /// <summary>
    /// 돼지 저금통들과의 거리를 계산해 멀어지려는 방향 벡터를 모아 반환
    /// </summary>
    private void PushOut()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius, Physics.DefaultRaycastLayers);

        foreach (Collider col in colliders)
        {
            // 자기 자신 제외
            if (col.gameObject == gameObject)
            {
                continue;
            }

            if (col.CompareTag("Wall"))
            {
                continue;
            }

            // 반구 형태이므로 중심보다 아래에 있는 오브젝트는 제외 
            if (col.transform.position.y < transform.position.y)
            {
                continue;
            }

            // 중심에서 오브젝트까지의 방향과 거리 계산
            Vector3 direction = col.transform.position - transform.position;
            direction.y = 0;
            float distance = direction.magnitude;

            // 거리가 반지름보다 작다면 (반구 내부에 있다면)
            if (distance < _radius && distance > 0)
            {
                Vector3 normalizedDir = direction / distance;
                Vector3 targetPos = transform.position + normalizedDir * _radius;

                // 내 위치에서 밀어내려는 목표 위치 사이에 벽이 있는지 레이캐스트로 검사
                if (Physics.Raycast(transform.position, normalizedDir, out RaycastHit wallHit, _radius))
                {
                    if (wallHit.collider.CompareTag("Wall"))
                    {
                        // 미는 경로(반경 내)에 벽이 있다면 벽을 뚫고 나가지 않도록 밀어내기 취소
                        continue;
                    }
                }

                // 벽이 가로막고 있지 않다면 안전하게 표면 위치로 이동
                col.transform.position = new Vector3(targetPos.x, col.transform.position.y, targetPos.z);
            }
        }
    }

    #endregion
}
