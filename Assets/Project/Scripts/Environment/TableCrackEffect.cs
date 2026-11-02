using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 책상이 공격받았을 때 균열 표시를 생성하고 관리
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TableCrackEffect : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    [SerializeField] private GameObject _crackPrefab;

    [Header("균열 설정")]
    [SerializeField] private float _surfaceOffset = 0.01f;
    [SerializeField] private float _minCrackScale = 0.85f;
    [SerializeField] private float _maxCrackScale = 1.15f;
    [SerializeField] private int _maxCrackCount = 5;

    [Header("회전 설정")]
    [SerializeField] private float _minRandomAngle = 0f;
    [SerializeField] private float _maxRandomAngle = 360f;
    #endregion

    #region 내부 변수
    private BoxCollider _tableCollider;
    private readonly List<GameObject> _createdCracks = new List<GameObject>();
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _tableCollider = GetComponent<BoxCollider>();

        if (_crackPrefab == null)
        {
            Debug.LogWarning("균열 프리팹이 없음.", this);
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 공격 위치 기준으로 책상 표면에 균열 생성
    /// </summary>
    /// <param name="attackCenter"></param>
    /// <returns></returns>
    public bool CreateCrack(Vector3 attackCenter)
    {
        if (_crackPrefab == null || _tableCollider == null)
        {
            return false;
        }

        if (_maxCrackCount <= 0)
        {
            return false;
        }

        if (!TryGetSurfacePoint(attackCenter, out Vector3 point, out Vector3 normal))
        {
            return false;
        }

        // 최대 생성 개수를 초과하면 가장 오래된 균열 제거
        while (_createdCracks.Count >= _maxCrackCount)
        {
            RemoveOldestCrack();
        }

        point += normal * _surfaceOffset;

        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, normal);

        // 표면의 법선을 기준으로 회전 방향 변경
        float randomAngle = Random.Range(_minRandomAngle, _maxRandomAngle);

        Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, normal);

        Quaternion rotation = randomRotation * surfaceRotation;

        // 균열 크기 무작위 설정
        float randomScale = Random.Range(_minCrackScale, _maxCrackScale);

        GameObject crack = Instantiate(_crackPrefab, point, rotation, transform);

        crack.transform.localScale *= randomScale;

        _createdCracks.Add(crack);

        return true;
    }

    /// <summary>
    /// 생성된 균열을 모두 제거
    /// </summary>
    public void ClearCracks()
    {
        for (int i = 0; i < _createdCracks.Count; i++)
        {
            if (_createdCracks[i] != null)
            {
                Destroy(_createdCracks[i]);
            }
        }

        _createdCracks.Clear();
    }
    #endregion

    #region 커스텀 (private)
    private bool TryGetSurfacePoint(Vector3 attackCenter, out Vector3 point, out Vector3 normal)
    {
        Bounds bounds = _tableCollider.bounds;

        Vector3 rayOrigin = new Vector3(attackCenter.x, bounds.max.y + 1f, attackCenter.z);

        Ray ray = new Ray(rayOrigin, Vector3.down);

        float rayDistance = bounds.size.y + 2f;

        if (_tableCollider.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = Vector3.zero;
        normal = Vector3.up;

        return false;
    }

    private void RemoveOldestCrack()
    {
        if (_createdCracks.Count == 0)
        {
            return;
        }

        GameObject oldestCrack = _createdCracks[0];

        if (oldestCrack != null)
        {
            Destroy(oldestCrack);
        }

        _createdCracks.RemoveAt(0);
    }
    #endregion

}
