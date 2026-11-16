using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PIG-AI] 돼지 저금통 움직임 관리
/// </summary>
public class PigBankMoveing : MonoBehaviour
{
    #region 상수
    /// <summary>
    /// 경직 스킬 ID (스킬 사용시 돼지 저금통 움직임 멈춤)
    /// </summary>
    private const string CStopSkillUpgradeId = "ACT_01";
    #endregion

    #region 인스펙터
    [Header("데이터 참조")]
    [SerializeField] private PigBankGameDataSO _pigData;

    [Header("점프 설정")]
    [SerializeField] private float _jumpHeight = 0.5f;
    [SerializeField] private float _jumpSpeed = 3f;

    [Header("겹침 설정")]
    [SerializeField] private float _radius = 3.5f;

    [Header("테이블 이동 영역 제한")]
    [SerializeField] private Vector2 _moveBounds = new Vector2(2.0f, 1.5f);
    #endregion

    #region 내부 변수
    private float _startY;
    private Vector3 _moveDir;
    private bool _isFrozenByActiveSkill;

    public float SpeedMultiplier { get; set; } = 1f;
    public bool IsStunned { get; set; } = false;
    private Coroutine _mechanismCoroutine;
    #endregion

    #region 이벤트
    private void OnEnable()
    {
        InitActiveSkillEvents();
        SyncFrozenState();
    }

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

        if (_isFrozenByActiveSkill)
        {
            return;
        }

        if (IsStunned)
        {
            return;
        }

        Moveing();
        PushOut();
        ConstrainToTableBounds();
    }

    private void OnDisable()
    {
        InitActiveSkillEvents(false);

        if (_mechanismCoroutine != null)
        {
            StopCoroutine(_mechanismCoroutine);
            _mechanismCoroutine = null;
        }

        _isFrozenByActiveSkill = false;
        SpeedMultiplier = 1f;
    }

    /// <summary>
    /// 에디터에서 반구 범위를 그리기 위한 기즈모
    /// </summary>
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawSphere(transform.position, _radius);
    //}

    /// <summary>
    /// 에디터에서 테이블 범위를 그리기 위한 기즈모
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 boxSize = new Vector3(_moveBounds.x * 2f, 0.1f, _moveBounds.y * 2f);
        Vector3 centerPos = new Vector3(0f, transform.position.y, 0f);

        Gizmos.DrawWireCube(centerPos, boxSize);
    }
    #endregion

    #region 커스텀 메서드 (public)
    public void RunMechanismCoroutine(IEnumerator routine)
    {
        if (_mechanismCoroutine != null)
        {
            StopCoroutine(_mechanismCoroutine);
        }

        _mechanismCoroutine = StartCoroutine(routine);
    }
    #endregion

    #region 커스텀 메서드 (private)

    private void ConstrainToTableBounds()
    {
        Vector3 pos = transform.position;
        bool hitWall = false;

        // X축(좌우) 경계 체크
        if (pos.x > _moveBounds.x)
        {
            pos.x = _moveBounds.x;
            _moveDir.x = -Mathf.Abs(_moveDir.x); // 왼쪽으로 방향 전환
            hitWall = true;
        }
        else if (pos.x < -_moveBounds.x)
        {
            pos.x = -_moveBounds.x;
            _moveDir.x = Mathf.Abs(_moveDir.x);  // 오른쪽으로 방향 전환
            hitWall = true;
        }

        // Z축(상하) 경계 체크
        if (pos.z > _moveBounds.y)
        {
            pos.z = _moveBounds.y;
            _moveDir.z = -Mathf.Abs(_moveDir.z); // 아래쪽으로 방향 전환
            hitWall = true;
        }
        else if (pos.z < -_moveBounds.y)
        {
            pos.z = -_moveBounds.y;
            _moveDir.z = Mathf.Abs(_moveDir.z);  // 위쪽으로 방향 전환
            hitWall = true;
        }

        if (hitWall)
        {
            _moveDir.y = 0;
            _moveDir.Normalize();
            transform.position = pos;
        }
    }

    private void Moveing()
    {
        if (_pigData == null)
        {
            Debug.LogError("PigData가 없다 확인 필요");
            return;
        }

        float currentSpeed = _pigData.MoveSpeed * SpeedMultiplier;
        Vector3 finalDir = _moveDir.normalized;

        float moveDiatance = currentSpeed * Time.deltaTime;
        float castDistance = moveDiatance + 0.2f;

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

            // 돼지 저금통이 아니면 밀어내지 않음 (바닥, 손, 무기 등 제외)
            if (!col.TryGetComponent<PigBankMoveing>(out var otherPig)) continue;

            Vector3 direction = col.transform.position - transform.position;
            direction.y = 0;
            float distance = direction.magnitude;

            if (distance < _radius && distance > 0.001f)
            {
                Vector3 normalizedDir = direction / distance;
                Vector3 targetPos = transform.position + normalizedDir * _radius;

                col.transform.position = new Vector3(targetPos.x, col.transform.position.y, targetPos.z);

                // 밀려난 상대방 돼지도 테이블 밖으로 나가지 않도록 즉시 경계 체크
                otherPig.ConstrainToTableBounds();
            }
        }
    }

    private void InitActiveSkillEvents(bool isAdd = true)
    {
        if (!ActiveSkillManager.HasInstance)
        {
            return;
        }

        ActiveSkillManager.Instance.OnActiveBuffStarted -= HandleActiveBuffStarted;
        ActiveSkillManager.Instance.OnActiveBuffRefreshed -= HandleActiveBuffRefreshed;
        ActiveSkillManager.Instance.OnActiveBuffEnded -= HandleActiveBuffEnded;

        if (isAdd)
        {

            ActiveSkillManager.Instance.OnActiveBuffStarted += HandleActiveBuffStarted;
            ActiveSkillManager.Instance.OnActiveBuffRefreshed += HandleActiveBuffRefreshed;
            ActiveSkillManager.Instance.OnActiveBuffEnded += HandleActiveBuffEnded;
        }
    }

    private void SyncFrozenState()
    {
        if (!ActiveSkillManager.HasInstance)
        {
            _isFrozenByActiveSkill = false;
            return;
        }

        _isFrozenByActiveSkill = ActiveSkillManager.Instance.IsBuffActive(CStopSkillUpgradeId);
    }

    private void ActionACT01(string upgradeId, bool isActive)
    {
        if (upgradeId != CStopSkillUpgradeId)
        {
            return;
        }

        _isFrozenByActiveSkill = isActive;
    }

    private void HandleActiveBuffStarted(string upgradeId, float duration)
    {
        ActionACT01(upgradeId, true);
    }

    private void HandleActiveBuffRefreshed(string upgradeId, float duration)
    {
        ActionACT01(upgradeId, true);
    }

    private void HandleActiveBuffEnded(string upgradeId)
    {
        ActionACT01(upgradeId, false);
    }

    #endregion
}
