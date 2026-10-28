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
    [Header("공격 범위")]
    [SerializeField] private PlayerStats _playerStats;

    [Header("입력 설정")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _groundLayer;

    [Header("책상 범위")]
    // 손과 에임이 머물 수 있는 책상의 콜라이더
    [SerializeField] private Collider _playAreaCollider;
    // 책상 가장자리에서 안쪽으로 띄우는 거리
    [SerializeField] private float _edgePadding = 0f;
    // 윗면으로 인정할 표면의 기울기 기준
    [SerializeField, Range(0f, 1f)] private float _minTopSurfaceNormalY = 0.7f;
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

        if (_playerStats == null)
        {
            _playerStats = GetComponentInParent<PlayerStats>();
        }

        // 첫 프레임부터 윗면 높이를 알게 하기
        if (_playAreaCollider != null)
        {
            _groundHeight = _playAreaCollider.bounds.max.y;
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
    /// 1. 광선이 책산 윗면에 닿으면 그 지점을 사용
    /// 2. 윗면에 닿지 않으면 마지막으로 닿은 윗면 놓이의 평면과 만나는 지점을 사용
    /// 3. 구한 위치를 책상 범위 안으로 제한해서 MouseWorldPosition에 저장
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

        if (TryRaycastTopSurface(ray, out RaycastHit hit))
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

    /// <summary>
    /// 광선이 책상 윗면에 닿았는지 검사
    /// 카메라가 비스듬히 보는 구조에서는 광선이 책상 앞면이나 바닥에 먼저 닿을 수 있으므로
    /// 표면이 위를 향하는 경우에만 지면으로 인정
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="hit"></param>
    /// <returns></returns>
    private bool TryRaycastTopSurface(Ray ray, out RaycastHit hit)
    {
        bool isHit = _playAreaCollider != null ? _playAreaCollider.Raycast(ray, out hit, Mathf.Infinity) : Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer, QueryTriggerInteraction.Ignore);

        // 임시 확인용: 윗면이 아닌 표면에 닿아서 무시되는 경우만 출력
        //if (isHit && hit.normal.y < _minTopSurfaceNormalY)
        //{
        //    Debug.Log($"무시된 표면: {hit.collider.name} / 법선 {hit.normal} / 높이 {hit.point.y:F2}");
        //}

        return isHit && hit.normal.y >= _minTopSurfaceNormalY;
    }

    /// <summary>
    /// 위치를 책상 콜라이어의 범위(X, Z) 안으로 제한
    /// </summary>
    /// <param name="point"></param>
    /// <param name="area"></param>
    /// <returns></returns>
    private Vector3 ClampToArea(Vector3 point, Collider area)
    {
        Bounds bounds = area.bounds;

        // 에임 원의 반지름 + 가장자리 여백
        float radius = _playerStats != null ? Mathf.Max(0f, _playerStats.AttackRange) : 0f;

        float padding = Mathf.Max(0f, _edgePadding);
        float margin = radius + padding;

        point.x = Mathf.Clamp(point.x, bounds.min.x + margin, bounds.max.x - margin);
        point.z = Mathf.Clamp(point.z, bounds.min.z + margin, bounds.max.z - margin);

        return point;
    }
    #endregion

}
