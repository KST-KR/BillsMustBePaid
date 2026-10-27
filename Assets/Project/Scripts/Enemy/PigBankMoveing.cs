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


    #endregion
}
