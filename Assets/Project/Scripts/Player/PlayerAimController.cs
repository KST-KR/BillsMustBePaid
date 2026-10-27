using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 망치를 내려찍을 에임 표시
/// 공격 범위 링 (점선): 지면의 마우스 위치에 공격 범위 크기로 표시되면 천천히 회전
/// 공격 진행 링 (실선): 공격 진행도에 따라 원형으로 채워지고, 전부 채워지면 망치가 내려찍음.
/// 중앙점: 마우스 포인터 위치. 어떤 오브젝트보다 위에 보이도록 화면 위에 따로 그림.
/// 스테이지 진행 중에만 보이며, 그 외에는 숨기고 시스템 커서를 다시 보여줌.
/// </summary>
public class PlayerAimController : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    [SerializeField] private PlayerStageActivator _activator;
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerCombat _combat;

    [Header("지면 에임")]
    // World Space Canvas의 Transform
    [SerializeField] private Transform _groundRoot;
    // 점선 공격 범위 링(천천히 회전)
    [SerializeField] private RectTransform _rangeRing;
    // 실선 링. Image Type을 Filled로 설정
    [SerializeField] private Image _fillRing;
    // 지면과 겹쳐 깜빡이는 현상 막기
    [SerializeField] private float _groundOffset = 0.02f;
    // 점선 링의 회전 속도 (초당 각도)
    [SerializeField] private float _rangeRotationSpeed = 20f;

    [Header("화면 에임 (Screen Space - Overlay Canvas")]
    // 중앙 점이 들어 있는 Overlay Canvas 오브젝트
    [SerializeField] private GameObject _screenRoot;
    // 마우스 위치를 따라다니는 중앙 점
    [SerializeField] private RectTransform _centerDot;
    // 에임이 보이는 동안 시스템 마우스 커서를 숨김
    [SerializeField] private bool _hideSystmeCursor = true;

    [Header("타격 연출")]
    // 타격 순간 에임이 커졌다가 돌아오는 비율
    [SerializeField] private float _hitPulseScale = 1.15f;
    [SerializeField] private float _hitPulseDuration = 0.15f;
    #endregion

    #region 내부 변수
    private bool _isShown;
    private bool _hasPosition;
    private float _pulseTimer;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (_activator == null)
        {
            _activator = GetComponentInParent<PlayerStageActivator>();
        }

        if (_inputHandler == null)
        {
            _inputHandler = GetComponentInParent<PlayerInputHandler>();
        }

        if (_playerStats == null)
        {
            _playerStats = GetComponentInParent<PlayerStats>();
        }

        if (_combat == null)
        {
            _combat = GetComponentInParent<PlayerCombat>();
        }
    }

    void Start()
    {
        if (_activator == null || _inputHandler == null || _playerStats == null || _combat == null)
        {
            Debug.LogError("필요한 참조가 없어 에임을 표시할 수 없음.");
            enabled = false;
            return;
        }

        _combat.OnAttackPerformed += HandleAttackPerformed;

        // 스테이지가 시작되기 전에는 숨김
        SetShown(false);
    }

    private void OnDestroy()
    {
        if (_combat != null)
        {
            _combat.OnAttackPerformed -= HandleAttackPerformed;
        }

        RestoreCursor();
    }

    private void OnDisable()
    {
        // 이 컴포넌트가 꺼지면 에임이 화면에 남지 않도록 정리
        if (_isShown)
        {
            SetShown(false);
        }
    }

    /// <summary>
    /// 입력 처리가 끝난 뒤의 마우스 위치를 반영
    /// </summary>
    private void LateUpdate()
    {
        bool shouldShow = _activator.CanAct;

        if (shouldShow != _isShown)
        {
            SetShown(shouldShow);
        }

        if (!_isShown)
        {
            return;
        }

        float pulse = UpdatePulse();

        UpdateGroundAim(pulse);
        UpdateCenterDot(pulse);
    }
    #endregion

    #region 커스텀 (private)
    private void HandleAttackPerformed(int hitCount)
    {
        _pulseTimer = _hitPulseDuration;
    }

    /// <summary>
    /// 타격 순간에 커졌다가 원래 크기로 돌아오는 비율 (1이면 원래 크기)
    /// </summary>
    private float UpdatePulse()
    {
        if (_pulseTimer <= 0f || _hitPulseDuration <= 0f)
        {
            return 1f;
        }

        _pulseTimer -= Time.deltaTime;

        float t = 1f - Mathf.Clamp01(_pulseTimer / _hitPulseDuration);

        return Mathf.Lerp(_hitPulseScale, 1f, t);
    }

    /// <summary>
    /// 지면의 마우스 위치에 공격 범위 크기로 링을 표시, 점성 링을 회전, 실선 링을 채움.
    /// </summary>
    /// <param name="pulse"></param>
    private void UpdateGroundAim(float pulse)
    {
        if (_groundRoot == null)
        {
            return;
        }

        // 커서가 지면에 한번도 닿지 않았다면 위치를 모르므로 숨김
        if (_inputHandler.HasMouseWorldPosition)
        {
            _hasPosition = true;
        }

        _groundRoot.gameObject.SetActive(_hasPosition);

        if (!_hasPosition)
        {
            return;
        }

        // 커서가 지면 밖으로 나가도 마지막 지면 위치를 유지 (손, 공격 판정과 같은 규칙)
        _groundRoot.position = _inputHandler.MouseWorldPosition + Vector3.up * _groundOffset;

        // 크기 : 지름 = 공격 범위x2 (월드 크기 1x1 캔버스를 지름으로 확대)
        float diameter = Mathf.Max(0.01f, _playerStats.AttackRange * 2f);
        _groundRoot.localScale = Vector3.one * diameter * pulse;

        if (_rangeRing != null)
        {
            _rangeRing.Rotate(0f, 0f, -_rangeRotationSpeed * Time.deltaTime, Space.Self);
        }

        if (_fillRing != null)
        {
            _fillRing.fillAmount = _combat.AimProgress;
        }
    }

    /// <summary>
    /// 중앙 점을 마우스 포인터 위치(화면 좌표)에 표시
    /// </summary>
    /// <param name="pulse"></param>
    private void UpdateCenterDot(float pulse)
    {
        if (_centerDot == null)
        {
            return;
        }

        _centerDot.position = Input.mousePosition;
        _centerDot.localScale = Vector3.one * pulse;
    }

    private void SetShown(bool isShown)
    {
        _isShown = isShown;

        if (_screenRoot != null)
        {
            _screenRoot.SetActive(isShown);
        }

        if (_groundRoot != null && !isShown)
        {
            _groundRoot.gameObject.SetActive(false);
        }

        if (isShown)
        {
            _pulseTimer = 0f;

            if (_fillRing != null)
            {
                _fillRing.fillAmount = 0f;
            }
        }

        if (_hideSystmeCursor)
        {
            Cursor.visible = !isShown;
        }
    }

    private void RestoreCursor()
    {
        if (_hideSystmeCursor)
        {
            Cursor.visible = true;
        }
    }
    #endregion
}
