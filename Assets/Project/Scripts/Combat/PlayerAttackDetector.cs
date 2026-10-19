using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerAttackDetector : MonoBehaviour
{
    #region 인스펙터
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
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 공격 판정
    /// </summary>
    public int PerformAttack()
    {
        Vector3 attackCenter = GetAttackCenter();
        float attackRange = Mathf.Max(0f, _playerStats.AttackRange);
        float damage = Mathf.Max(0f, _playerStats.AttackPower);

        // 구형 범위 안의 콜라이더를 찾는다.
        Collider[] colliders = Physics.OverlapSphere(attackCenter, attackRange, _targetLayers, QueryTriggerInteraction.Collide);

        // 같은 대상이 중복 타격되는 것을 방지
        HashSet<IPlayerAttackTarget> hitTargets = new HashSet<IPlayerAttackTarget>();

        int hitCount = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            MonoBehaviour[] components = colliders[i].GetComponentsInParent<MonoBehaviour>();

            for (int j = 0; j < components.Length; j++)
            {
                if (!(components[j] is IPlayerAttackTarget target))
                {
                    continue;
                }

                // 같은 대상은 한 번의 공격에서 한 번만 타격한다.
                if (!hitTargets.Add(target))
                {
                    break;
                }

                target.ReceivePlayerAttack(damage);
                hitCount++;

                // 가장 가까운 부모 대상 하나만 처리한다.
                break;
            }
        }

        return hitCount;
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 공격 범위
    /// </summary>
    private Vector3 GetAttackCenter()
    {
        return transform.position + transform.forward * _forwardOffset + Vector3.up * _heightOffset;
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
