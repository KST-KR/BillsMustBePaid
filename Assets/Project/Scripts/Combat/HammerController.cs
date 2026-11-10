using UnityEngine;

/// <summary>
/// 망치의 장착 및 분리, 낙하 처리
/// </summary>
public class HammerController : MonoBehaviour
{
    #region 인스펙터
    [Header("장비 참조")]
    [SerializeField] private PlayerEquipment _playerEquipment;
    #endregion

    #region 내부 변수
    private Transform _currentHammer;
    private Rigidbody _rigidbody;
    private Collider _collider;

    private Vector3 _originalLocalPosition;
    private Quaternion _originalLocalRotation;
    private Vector3 _originalLocalScale;

    private bool _isDropped;
    #endregion

    #region 프로퍼티
    public bool IsDropped => _isDropped;
    #endregion

    #region 커스텀 메서드 (Private)
    private bool RefreshHammerReference()
    {
        if (_playerEquipment == null)
        {
            Debug.LogWarning("PlayerEquipment가 연결되지 않음.");
            return false;
        }

        _currentHammer = _playerEquipment.CurrentHammerTransform;

        if (_currentHammer == null)
        {
            Debug.LogWarning("현재 장착된 망치를 찾을 수 없음.");
            return false;
        }

        _rigidbody = _currentHammer.GetComponent<Rigidbody>();
        _collider = _currentHammer.GetComponent<Collider>();

        if (_rigidbody == null || _collider == null)
        {
            Debug.LogWarning($"{_currentHammer.name}: Rigidbody 또는 Collider가 없음.");
            return false;
        }

        _originalLocalPosition = _currentHammer.localPosition;
        _originalLocalRotation = _currentHammer.localRotation;
        _originalLocalScale = _currentHammer.localScale;

        return true;
    }

    /// <summary>
    /// 손에 장착된 상태의 물리 설정
    /// </summary>
    private void SetAttachedPhysics()
    {
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _collider.enabled = false;
    }
    #endregion

    #region 커스텀 메서드 (Public)
    /// <summary>
    /// 망치를 손에서 분리하고 떨어뜨림
    /// </summary>
    public void DropHammer()
    {
        if (_isDropped || !RefreshHammerReference())
        {
            return;
        }

        _isDropped = true;

        // 부모 연결 해제 (월드 위치 유지)
        _currentHammer.SetParent(null, true);

        _collider.enabled = true;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;

        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        Debug.Log("망치 분리 및 낙하");
    }

    /// <summary>
    /// 망치를 원래 손 위치에 다시 장착
    /// </summary>
    public void AttachHammer()
    {
        if (_currentHammer == null || !_isDropped)
            return;

        _isDropped = false;

        // 물리 움직임을 먼저 중단
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _collider.enabled = false;

        // 원래 부모와 위치 복원
        _currentHammer.SetParent(transform, false);
        _currentHammer.localPosition = _originalLocalPosition;
        _currentHammer.localRotation = _originalLocalRotation;
        _currentHammer.localScale = _originalLocalScale;

        Debug.Log("망치 재장착");
    }
    #endregion
}
