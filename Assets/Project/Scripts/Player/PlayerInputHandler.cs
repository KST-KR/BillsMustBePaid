using UnityEngine;

/// <summary>
/// [PL-MOVE-01] 마우스 입력만 처리
/// 실제 이동속도나 위치 변경은 담당하지 않음.
/// 마우스가 책상 밖으로 나가면, 손과 에임이 쓰는 위치는 책상 범위 안에서 커서에 가장 가까운 위치로 제한되어 가장자리를 따라 부드럽게 움직임.
/// 책상 밖의 실제 마우스 위치는 RawMouseWorldPosition으로 얻을 수 있음. (중앙 점은 화면 좌표를 직접 따라가므로 책상 밖으로 나갈 수 있음)
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    #region 인스펙터
    [Header("입력 설정")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _groundLayer;

    [Header("책상 범위")]
    // 손과 에임이 머물 수 있는 책상의 콜라이더
    [SerializeField] private Collider _playAreaCollider;
    // 책상 가장자리에서 안쪽으로 띄우는 거리
    [SerializeField] private float _edgePadding = 0f;
    #endregion

    #region 내부 변수
    // 책상 밖에서 마우스 위치를 계산할 때 기준이 되는 평면의 높이
    private float _groundHeight;
    // 마지막으로 마우스가 닿은 지면 콜라이더
    private Collider _lastGroundCollider;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 손, 에임, 공격 판정이 쓰는 위치
    /// 마우스가 책상 밖에 있어도 책상 범위 안에서 커서에 가장 가까운 위치
    /// </summary>
    public Vector3 MouseWorldPosition { get; private set; }

    /// <summary>
    /// 책상 범위를 제한하지 않은 마우스의 실제 월드 위치
    /// </summary>
    public Vector3 RawMouseWorldPosition { get; private set; }

    /// <summary>
    /// 이번 프레임에 마우스 위치를 계산할 수 있었는지
    /// 책상 범위를 아직 모르거나 마우스가 지면 평면과 만나지 않으면 false
    /// </summary>
    public bool HasMouseWorldPosition { get; private set; }

    /// <summary>
    /// 마우스가 실제 지면 콜라이더 위에 있는지
    /// </summary>
    public  bool IsMouseOnGround { get; private set; }
    #endregion

    #region 이벤트
    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        UpdateMouseWorldPosition();
    }

    void Update()
    {
        UpdateMouseWorldPosition();
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 마우스 입력 값에 따라 업데이트
    /// 1. 지면 콜라이더에 닿으면 그 지점, 닿지 않으면 마지막으로 닿은 지면 높이의 평면과 만나는 지점을 구함.
    /// 2. 구한 위치를 책상 범위 안으로 제한해서 MouseWorldPosition에 저장
    /// </summary>
    private void UpdateMouseWorldPosition()
    {
        HasMouseWorldPosition = false;
        IsMouseOnGround = false;

        if (_mainCamera == null)
        {
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 point;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer))
        {
            point = hit.point;
            IsMouseOnGround = true;

            _groundHeight = hit.point.y;
            _lastGroundCollider = hit.collider;
        }
        else 
        {
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, _groundHeight, 0f));

            if (!groundPlane.Raycast(ray, out float enter))
            {
                return;
            }

            point = ray.GetPoint(enter);
        }

        Collider area = _playAreaCollider != null ? _playAreaCollider : _lastGroundCollider;

        // 지면에 한번도 닿지 않아 책상 범위를 알 수 없으면 손이 엉뚱한 곳으로 가지 않도록 위치를 내보내지 않음.
        if (area == null)
        {
            return;
        }

        RawMouseWorldPosition = point;
        MouseWorldPosition = ClampToArea(point, area);
        HasMouseWorldPosition = true;
    }

    private Vector3 ClampToArea(Vector3 point, Collider area)
    {
        Bounds bounds = area.bounds;
        float padding = Mathf.Max(0f, _edgePadding);

        point.x = Mathf.Clamp(point.x, bounds.min.x + padding, bounds.max.x - padding);
        point.z = Mathf.Clamp(point.z, bounds.min.z + padding, bounds.max.z - padding);

        return point;
    }
    #endregion

}
