using TMPro;
using UnityEngine;

/// <summary>
/// 플레이어의 최종 능력치를 공통 팝업 UI에 표시
/// PlayerStats의 능력치 변경 이벤트를 구독하여 UI를 갱신
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    #region 인스펙터
    [Header("플레이어 스테이터스")]
    [SerializeField] private PlayerStats _playerStats;

    [Header("스테이터스 텍스트")]
    [SerializeField] private TMP_Text _attackPowerText;
    [SerializeField] private TMP_Text _attackIntervalText;
    [SerializeField] private TMP_Text _attackRangeText;
    [SerializeField] private TMP_Text _criticalChanceText;
    [SerializeField] private TMP_Text _criticalDamageText;
    [SerializeField] private TMP_Text _luckText;
    [SerializeField] private TMP_Text _maxStaminaText;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        // 시작할 때 팝업 숨김
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (_playerStats == null)
        {
            Debug.LogError("PlayerStats가 연결되지 않음.");
            return;
        }

        _playerStats.OnStateChanged += HandleStateChanged;

        // 팝업이 다시 열렸을 때 현재 능력치를 반영
        UpdateStatsUI();
    }

    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.OnStateChanged -= HandleStateChanged;
        }
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// PlayerStats의 능력치 변경 이벤트 처리
    /// </summary>
    private void HandleStateChanged()
    {
        UpdateStatsUI();
    }

    /// <summary>
    /// 현재 최종 능력치를 UI에 반영
    /// </summary>
    private void UpdateStatsUI()
    {
        if (_playerStats == null)
        {
            return;
        }

        if (_attackPowerText != null)
        {
            _attackPowerText.text = NumberFormatter.FormatTrimmed(_playerStats.AttackPower);
        }

        if (_attackIntervalText != null)
        {
            _attackIntervalText.text = $"{NumberFormatter.FormatTrimmed(_playerStats.AttackInterval)}초";
        }

        if (_attackRangeText != null)
        {
            _attackRangeText.text = NumberFormatter.FormatTrimmed(_playerStats.AttackRange);
        }

        if (_criticalChanceText != null)
        {
            _criticalChanceText.text = NumberFormatter.FormatPercent(_playerStats.CriticalChance);
        }

        if (_criticalDamageText != null)
        {
            _criticalDamageText.text = $"{NumberFormatter.FormatTrimmed(_playerStats.CriticalDamageMultiplier)}배";
        }

        if (_luckText != null)
        {
            _luckText.text = NumberFormatter.FormatTrimmed(_playerStats.Luck);
        }

        if (_maxStaminaText != null)
        {
            _maxStaminaText.text = NumberFormatter.FormatTrimmed(_playerStats.MaxStamina);
        }
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 지정한 망치 아이콘 아래에 팝업 표시
    /// /// 화면 경계를 벗어나지 않도록 위치를 보정
    /// </summary>
    /// <param name="anchor"></param>
    public void Show(RectTransform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        gameObject.SetActive(true);

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            return;
        }

        RectTransform popupRect = transform as RectTransform;
        RectTransform parentRect = popupRect.parent as RectTransform;

        if (popupRect == null || parentRect == null)
        {
            return;
        }

        // 레이아웃 갱신 후 팝업 크기 측정
        Canvas.ForceUpdateCanvases();

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 팝업의 화면상 크기 계산
        Vector3[] popupCorners = new Vector3[4];
        popupRect.GetWorldCorners(popupCorners);

        Vector2 popupBottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, popupCorners[0]);

        Vector2 popupTopRight = RectTransformUtility.WorldToScreenPoint(uiCamera, popupCorners[2]);

        float popupWidth = popupTopRight.x - popupBottomLeft.x;
        float popupHeight = popupTopRight.y - popupBottomLeft.y;

        // 아이콘의 화면 좌표 계산
        Vector3[] anchorCorners = new Vector3[4];
        anchor.GetWorldCorners(anchorCorners);

        Vector2 bottomCenter = RectTransformUtility.WorldToScreenPoint(uiCamera, (anchorCorners[0] + anchorCorners[3]) * 0.5f);

        Vector2 topCenter = RectTransformUtility.WorldToScreenPoint(uiCamera, (anchorCorners[1] + anchorCorners[2]) * 0.5f);

        const float screenPadding = 10f;
        const float popupGap = 8f;

        // 아래쪽과 위쪽에 표시할 수 있는 공간 계산
        float spaceBelow = bottomCenter.y - popupGap - screenPadding;
        float spaceAbove = Screen.height - topCenter.y - popupGap - screenPadding;

        bool showBelow = spaceBelow >= popupHeight;

        // 위아래 어느 쪽도 충분하지 않으면 공간이 더 넓은 쪽 선택
        if (!showBelow && spaceAbove < popupHeight)
        {
            showBelow = spaceBelow >= spaceAbove;
        }

        // 팝업의 Pivot 설정
        popupRect.pivot = showBelow ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);

        // 화면 좌우 경계를 벗어나지 않도록 X 좌표 보정
        float halfWidth = popupWidth * 0.5f;

        float targetX = Mathf.Clamp(bottomCenter.x, screenPadding + halfWidth, Screen.width - screenPadding - halfWidth);

        // 화면 위아래 경계를 벗어나지 않도록 Y 좌표 보정
        float targetY;

        if (showBelow)
        {
            targetY = Mathf.Clamp(bottomCenter.y - popupGap, screenPadding + popupHeight, Screen.height - screenPadding);
        }
        else
        {
            targetY = Mathf.Clamp(topCenter.y + popupGap, screenPadding, Screen.height - screenPadding - popupHeight);
        }

        Vector2 targetScreenPosition = new Vector2(targetX, targetY);

        // 화면 좌표를 팝업 부모의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPosition, uiCamera, out Vector2 localPosition);

        popupRect.anchoredPosition = localPosition;

        UpdateStatsUI();
    }

    /// <summary>
    /// 팝업 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    #endregion

}
