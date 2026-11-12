using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레거시 유물 HUD의 표시 상태 툴팁 관리
/// </summary>
public sealed class LegacyRelicHud : MonoBehaviour
{
    #region 상수
    private const float CTooltipWidth = 560.0f;
    private const float CScreenEdgePadding = 12.0f; 
    #endregion

    #region 인스펙터
    [Header("구매 상태")]
    [SerializeField] private LegacyShopManager _legacyShopManager;
    [SerializeField] private bool _isHammerOwned;

    [Header("HUD 계층")]
    [SerializeField] private GameObject _ringIconObject;
    [SerializeField] private GameObject _ringPopupObject;
    [SerializeField] private RectTransform _ringPopupRect;
    [SerializeField] private LegacyRelicHudTile[] _ringTiles;

    [Header("툴팁")]
    [SerializeField] private RectTransform _tooltipRect;
    [SerializeField] private TMP_Text _tooltipTitle;
    [SerializeField] private TMP_Text _tooltipDescription;
    #endregion

    #region 내부 변수
    private GameManager _gameManager;
    private BillManager _billManager;
    private Canvas _canvas;
    private Coroutine _hidePopupCoroutine;
    private Coroutine _hideTooltipCoroutine;
    private bool _isEventVisible; 
    #endregion

    /// <summary>
    /// 계층의 참조를 연결
    /// </summary>
    public void Configure(LegacyShopManager manager, GameObject hammerIcon, GameObject ringIcon,
        GameObject ringPopup, RectTransform popupRect, LegacyRelicHudTile[] tiles,
        RectTransform tooltip, TMP_Text tooltipTitle, TMP_Text tooltipDescription)
    {
        _legacyShopManager = manager;
        _ringIconObject = ringIcon;
        _ringPopupObject = ringPopup;
        _ringPopupRect = popupRect;
        _ringTiles = tiles;
        _tooltipRect = tooltip;
        _tooltipTitle = tooltipTitle;
        _tooltipDescription = tooltipDescription;
    }

    #region 이벤트 함수
    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        SubscribeEvents();
        RefreshOwnedItems();
        RefreshVisibility();
    }

    private void OnDestroy()
    {
        if (_legacyShopManager != null)
            _legacyShopManager.OnOwnershipDataChanged -= HandleOwnershipChanged;
        if (_gameManager != null)
            _gameManager.OnGameStateChanged -= HandleVisibilityChanged;
        if (_billManager != null)
        {
            _billManager.OnBillExpired -= HandleBillChanged;
            _billManager.OnBillPaid -= HandleBillChanged;
        }
    }
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// 망치 구매 시스템에서 실제 소유 상태를 전달
    /// </summary>
    public void SetHammerOwned(bool isOwned)
    {
        _isHammerOwned = isOwned;
        RefreshVisibility();
    }

    public void ShowItemTooltip(LegacyShopItemData item, RectTransform iconRect)
    {
        if (!_isEventVisible || item == null || _tooltipRect == null)
        {
            return;
        }

        CancelHidePopup();
        CancelHideTooltip();

        _tooltipTitle.text = item.DisplayName;
        _tooltipDescription.text = item.Description + "\n\n" + item.StatEffectText;

        float scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1.0f;
        float width = Mathf.Min(CTooltipWidth, (Screen.width - CScreenEdgePadding * 2.0f) / scale);

        _tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        _tooltipDescription.ForceMeshUpdate();
        float contentHeight = _tooltipDescription.GetPreferredValues(_tooltipDescription.text, _tooltipDescription.rectTransform.rect.width, 0.0f).y;

        _tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(contentHeight + 82.0f, 170.0f, 420.0f));
        PositionTooltip(iconRect);
        _tooltipRect.gameObject.SetActive(true);
    }

    public void ScheduleTooltipHide()
    {
        CancelHideTooltip();
        _hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay());
    }

    public void OnRingIconPointerEnter()
    {
        CancelHidePopup();
        if (!_isEventVisible || _ringPopupObject == null) return;
        RefreshOwnedItems();
        _ringPopupObject.SetActive(true);
    }

    public void OnRingIconPointerExit()
    {
        SchedulePopupHide();
    }

    public void OnPopupPointerEnter()
    {
        CancelHidePopup();
        CancelHideTooltip();
    }

    public void OnPopupPointerExit()
    {
        SchedulePopupHide();
    }
    #endregion

    #region 커스텀 (Private)
    private void SubscribeEvents()
    {
        _legacyShopManager = LegacyShopManager.Instance != null ? LegacyShopManager.Instance : _legacyShopManager;
        _gameManager = GameManager.Instance;
        _billManager = BillManager.Instance;

        if (_legacyShopManager != null)
        {
            _legacyShopManager.OnOwnershipDataChanged -= HandleOwnershipChanged;
            _legacyShopManager.OnOwnershipDataChanged += HandleOwnershipChanged;
        }

        if (_gameManager != null)
        {
            _gameManager.OnGameStateChanged -= HandleVisibilityChanged;
            _gameManager.OnGameStateChanged += HandleVisibilityChanged;
        }

        if (_billManager != null)
        {
            _billManager.OnBillExpired -= HandleBillChanged;
            _billManager.OnBillPaid -= HandleBillChanged;
            _billManager.OnBillExpired += HandleBillChanged;
            _billManager.OnBillPaid += HandleBillChanged;
        }
    }

    private void HandleOwnershipChanged()
    {
        RefreshOwnedItems();
    }

    private void HandleVisibilityChanged(EGameState state)
    {
        RefreshVisibility();
    }

    private void HandleBillChanged(BillRecord bill)
    {
        RefreshVisibility();
    }

    private void RefreshOwnedItems()
    {
        if (_legacyShopManager == null)
        {
            _legacyShopManager = LegacyShopManager.Instance;
        }

        if (_ringTiles == null)
        {
            return;
        }

        for (int i = 0; i < _ringTiles.Length; i++)
        {
            LegacyRelicHudTile tile = _ringTiles[i];
            if (tile != null)
            {
                tile.SetOwned(_legacyShopManager != null && _legacyShopManager.IsOwned(tile.Item));
            }
        }

        if (_ringPopupRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_ringPopupRect);
        }
    }

    private void RefreshVisibility()
    {
        if (_gameManager == null) _gameManager = GameManager.Instance;
        if (_billManager == null) _billManager = BillManager.Instance;

        _isEventVisible = (_gameManager != null && _gameManager.State == EGameState.StageClear) ||
            (_billManager != null && _billManager.HasExpiredBills);

        bool hasRings = HasOwnedRing();

        if (_ringIconObject != null)
        {
            _ringIconObject.SetActive(_isEventVisible && hasRings);
        }

        if (!_isEventVisible && _ringPopupObject != null)
        {
            _ringPopupObject.SetActive(false);
            if (_tooltipRect != null)
            {
                _tooltipRect.gameObject.SetActive(false);
            }
        }
    }

    private bool HasOwnedRing()
    {
        if (_ringTiles == null) return false;
        for (int i = 0; i < _ringTiles.Length; i++)
        {
            if (_ringTiles[i] != null && _ringTiles[i].gameObject.activeSelf) return true;
        }
        return false;
    }

    private void PositionTooltip(RectTransform iconRect)
    {
        if (iconRect == null || _canvas == null || _ringPopupRect == null) return;
        Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        //Vector2 trialPosition = iconRect.anchoredPosition + new Vector2(100.0f, -20.0f);
        //if (trialPosition.x + _tooltipRect.rect.width > _ringPopupRect.rect.width)
        //{
        //    trialPosition.x = iconRect.anchoredPosition.x - _tooltipRect.rect.width - 16.0f;
        //}
        //if (trialPosition.y - _tooltipRect.rect.height < -Screen.height * 0.5f)
        //{
        //    trialPosition.y = iconRect.anchoredPosition.y + _tooltipRect.rect.height;
        //}
        //_tooltipRect.anchoredPosition = trialPosition;

        Vector3[] corners = new Vector3[4];
        iconRect.GetWorldCorners(corners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

        float scale = _canvas.scaleFactor;
        float width = _tooltipRect.rect.width * scale;
        float height = _tooltipRect.rect.height * scale;
        float left = topRight.x + CScreenEdgePadding;

        if (left + width > Screen.width - CScreenEdgePadding)
        {
            left = bottomLeft.x - width - CScreenEdgePadding;
        }

        left = Mathf.Clamp(left, CScreenEdgePadding, Mathf.Max(CScreenEdgePadding, Screen.width - width - CScreenEdgePadding));

        float top = topRight.y;
        if (top - height < CScreenEdgePadding)
        {
            top = bottomLeft.y + height;
        }

        top = Mathf.Clamp(top, height + CScreenEdgePadding, Screen.height - CScreenEdgePadding);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_ringPopupRect, new Vector2(left, top), eventCamera, out Vector2 localPoint))
        {
            _tooltipRect.anchoredPosition = localPoint;
        }
    }

    private void SchedulePopupHide()
    {
        CancelHidePopup();
        _hidePopupCoroutine = StartCoroutine(HidePopupAfterDelay());
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.15f);

        if (_ringPopupObject != null)
        {
            _ringPopupObject.SetActive(false);
        }

        if (_tooltipRect != null)
        {
            _tooltipRect.gameObject.SetActive(false);
        }

        _hidePopupCoroutine = null;
    }

    private IEnumerator HideTooltipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.12f);

        if (_tooltipRect != null)
        {
            _tooltipRect.gameObject.SetActive(false);
        }

        _hideTooltipCoroutine = null;
    }

    private void CancelHidePopup()
    {
        if (_hidePopupCoroutine == null)
        {
            return;
        }

        StopCoroutine(_hidePopupCoroutine);
        _hidePopupCoroutine = null;
    }

    private void CancelHideTooltip()
    {
        if (_hideTooltipCoroutine == null)
        {
            return;
        }

        StopCoroutine(_hideTooltipCoroutine);
        _hideTooltipCoroutine = null;
    } 
    #endregion
}
