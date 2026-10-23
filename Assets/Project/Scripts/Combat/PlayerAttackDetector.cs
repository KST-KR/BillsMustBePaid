using System.Collections;
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
    /// 공격 범위의 중심
    /// 손은 지면 위 높이(PlayerHandPosition)에 떠 있으므로, 손 위치가 아니라
    /// 마우스가 가리키는 지면 좌표를 기준으로 판정한다. (커서가 지면을 벗어나면 마지막 지면 좌표 유지)
    /// </summary>
    private Vector3 GetAttackCenter()
    {
        if (_inputHandler != null && Application.isPlaying)
        {
            return _inputHandler.MouseWorldPosition + Vector3.up * _heightOffset;
        }

        // 입력 참조가 없거나 에디터(재생 전)에서는 이 오브젝트 위치 기준
        return transform.position + Vector3.up * _heightOffset;
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
