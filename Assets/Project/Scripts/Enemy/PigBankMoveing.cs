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

    [Header("통통 튀는 설정")]
    [SerializeField] private float _jumpHeight = 0.5f;
    [SerializeField] private float _jumpSpeed = 3f;
    #endregion

    #region 내부 변수
    private Rigidbody _rd;
    private float _startY;
    #endregion

    #region 이벤트
    private void Awake()
    {
        _rd = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (_pigData == null)
        {
            Debug.LogError("PigData가 없다 확인 필요");
            return;
        }

        if (_rd == null)
        {
            Debug.Log("Rigidbody가 없음 확인 필요");
            return;
        }

        _startY = transform.position.y;
    }


    void Update()
    {
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

        if (_rd == null)
        {
            Debug.Log("Rigidbody가 없음 확인 필요");
            return;
        }

        // TODO : Y축 점프는 구현 성공 앞으로 안가는 문제를 해결해야 함

        float currentSpeed = _pigData.MoveSpeed;

        Vector3 moverDir = Vector3.forward;
        Vector3 nextVelocity = moverDir * currentSpeed;

        _rd.velocity = new Vector3(nextVelocity.x * _rd.velocity.y, nextVelocity.z);

        float newY = _startY + Mathf.Abs(Mathf.Sin(Time.time * _jumpSpeed)) * _jumpHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // TODO : 막다른 벽을 만나면 다른 각도로 움직일 수 있게 해주어야 함
    }
    #endregion
}
