using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerAttackDetector : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    [Tooltip("마우스가 가리키는 지면 좌표 제공, 비워두면 같은 오브젝트나 부모에서 찾음.")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    
    [Header("공격 범위 위치")]
    [SerializeField] private float _forwardOffset = 1f;
    [SerializeField] private float _heightOffset = 0.5f;

    [Header("대상 탐색")]
    // 공격 대상이 될 레이어 제한
    [SerializeField] private LayerMask _targetLayers = ~0;
    #endregion

    #region 내부 변수
    private PlayerStats _playerStats;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();

        if (_inputHandler == null)
        {
            _inputHandler = GetComponentInParent<PlayerInputHandler>();
        }

        if (_inputHandler == null)
        {
            Debug.LogWarning("PlayerInputHandler를 찾을 수 없음.");
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 공격 범위 안의 대상을 탐색하고 공격한다.
    /// 한 번의 공격에서 대상 하나당 한 번만 타격한다.
    /// </summary>
    public int PerformAttack()
    {
        Vector3 attackCenter = GetAttackCenter();
        float attackRange = Mathf.Max(0f, _playerStats.AttackRange);
        float damage = Mathf.Max(0f, _playerStats.AttackPower);

        // 구형 범위 안의 콜라이더를 찾는다.
        Collider[] colliders = Physics.OverlapSphere(attackCenter, attackRange, _targetLayers, QueryTriggerInteraction.Collide);

        Debug.Log($"[공격 탐색] 중심={attackCenter}, " +$"반경={attackRange}, " +$"검색된 콜라이더={colliders.Length}", this);

        // 같은 대상이 중복 타격되는 것을 방지
        HashSet<IPlayerAttackTarget> hitTargets = new HashSet<IPlayerAttackTarget>();

        int hitCount = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            IPlayerAttackTarget target = FindAttackTarget(colliders[i]);

            Debug.Log( $"[공격 대상 확인] 콜라이더={colliders[i].name}, " +$"레이어={LayerMask.LayerToName(colliders[i].gameObject.layer)}, " +$"공격 대상={target != null}",colliders[i]);

            if (target == null || !hitTargets.Add(target))
            {
                continue;
            }

            target.TakePlayerAttack(damage);
            hitCount++;
        }

        return hitCount;
    }
    #endregion

    #region 커스텀 (private)
    private IPlayerAttackTarget FindAttackTarget(Collider targetcollider)
    {
        if (targetcollider == null)
        {
            return null;
        }

        MonoBehaviour[] components = targetcollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IPlayerAttackTarget target)
            {
                return target;
            }
        }

        return null;
    }

    /// <summary>
    /// 공격 범위의 중심
    /// 손은 지면 위 높이(PlayerHandPosition)에 떠 있으므로, 손 위치가 아님.
    /// 마우스가 가리키는 지면 좌표를 기준으로 판정한다. (커서가 지면을 벗어나면 마지막 지면 좌표 유지)
    /// </summary>
    private Vector3 GetAttackCenter()
    {
        Vector3 attackCenter;

        if (_inputHandler != null && Application.isPlaying)
        {
            Vector3 mousePosition = _inputHandler.MouseWorldPosition;

            // 플레이어에서 마우스 지면 좌표로 향하는 방향
            Vector3 direction = mousePosition - transform.position;

            // 지면 기준으로 계산하므로 높이 차이는 제외
            direction.y = 0f;

            if (direction.sqrMagnitude > 0f)
            {
                direction.Normalize();
            }

            attackCenter = mousePosition + direction * Mathf.Max(0f, _forwardOffset);
        }
        else
        {
            // 입력 참조가 없거나 에디터(재생 전)에서는 이 오브젝트 위치 기준
            attackCenter = transform.position + transform.forward * Mathf.Max(0f, _forwardOffset);
        }

        attackCenter.y += _heightOffset;

        return attackCenter;
    }

    #endregion

    #region GUI
    /// <summary>
    ///  Scene뷰에서 공격 범위 확인
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        PlayerStats stats = GetComponent<PlayerStats>();

        float range = stats != null ? Mathf.Max(0f, stats.AttackRange) : 1f;

        Vector3 center = GetAttackCenter();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, range);
    }
    #endregion
}
