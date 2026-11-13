using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject _hammerIconObject;
    [SerializeField] private GameObject _ringIconObject;
    [SerializeField] private GameObject _perkIconObject;
    [SerializeField] private GameObject _ringPopupObject;
    [SerializeField] private RectTransform _ringPopupRect;
    [SerializeField] private LegacyRelicHudTile[] _ringTiles;
    [SerializeField] private GameObject _perkPopupObject;
    [SerializeField] private RectTransform _perkPopupRect;
    [SerializeField] private RectTransform _perkGridRect;
    [SerializeField] private PerkHudTile[] _perkTiles;
    [SerializeField] private Sprite _perkDefaultIcon;

    [Header("툴팁")]
    [SerializeField] private RectTransform _tooltipRect;
    [SerializeField] private TMP_Text _tooltipTitle;
    [SerializeField] private TMP_Text _tooltipDescription;
    #endregion

    #region 내부 변수
    private GameManager _gameManager;
    private BillManager _billManager;
    private PerkManager _perkManager;
    private Canvas _canvas;
    private Coroutine _hidePopupCoroutine;
    private Coroutine _hidePerkPopupCoroutine;
    private Coroutine _hideTooltipCoroutine;
    private bool _isEventVisible; 
    private readonly List<PerkHudTile> _dynamicPerkTiles = new List<PerkHudTile>();
    #endregion

    /// <summary>
    /// 계층의 참조를 연결
    /// </summary>
    public void Configure(LegacyShopManager manager, GameObject hammerIcon, GameObject ringIcon,
        GameObject ringPopup, RectTransform popupRect, LegacyRelicHudTile[] tiles,
        RectTransform tooltip, TMP_Text tooltipTitle, TMP_Text tooltipDescription)
    {
        _legacyShopManager = manager;
        _hammerIconObject = hammerIcon;
        _ringIconObject = ringIcon;
        _ringPopupObject = ringPopup;
        _ringPopupRect = popupRect;
        _ringTiles = tiles;
        _tooltipRect = tooltip;
        _tooltipTitle = tooltipTitle;
        _tooltipDescription = tooltipDescription;
    }

    public void ConfigurePerkHud(GameObject perkIconObject, GameObject perkPopupObject, RectTransform perkPopupRect,
        PerkHudTile[] perkTiles, Sprite perkDefaultIcon)
    {
        _perkIconObject = perkIconObject;
        _perkPopupObject = perkPopupObject;
        _perkPopupRect = perkPopupRect;
        _perkGridRect = perkPopupRect;
        _perkTiles = perkTiles;
        _perkDefaultIcon = perkDefaultIcon;
    }

    #region 이벤트 함수
    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        SubscribeEvents();
        RefreshOwnedItems();
        SyncPerkPopupLayoutFromRing();
        RefreshOwnedPerks();
        RefreshVisibility();
    }

    private void OnDestroy()
    {
        if (_legacyShopManager != null)
            _legacyShopManager.OnOwnershipDataChanged -= HandleOwnershipChanged;
        if (_perkManager != null)
            _perkManager.OnPerkStateChanged -= HandlePerkStateChanged;
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

        EnsureTooltipParent(_ringPopupRect);

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
        PositionTooltip(iconRect, _ringPopupRect);
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

    public void ShowPerkTooltip(PerkData perk, RectTransform iconRect)
    {
        if (perk == null || _tooltipRect == null)
        {
            return;
        }

        if (!_isEventVisible && (_perkPopupObject == null || !_perkPopupObject.activeSelf))
        {
            return;
        }

        EnsureTooltipParent(_perkPopupRect != null ? _perkPopupRect : _ringPopupRect);

        CancelHidePerkPopup();
        CancelHideTooltip();

        _tooltipTitle.text = perk.DisplayName;
        _tooltipDescription.text = perk.EffectText + "\n" + "TIER " + perk.Tier;

        float scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1.0f;
        float width = Mathf.Min(CTooltipWidth, (Screen.width - CScreenEdgePadding * 2.0f) / scale);
        _tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        _tooltipDescription.ForceMeshUpdate();
        float contentHeight = _tooltipDescription.GetPreferredValues(_tooltipDescription.text, _tooltipDescription.rectTransform.rect.width, 0.0f).y;

        _tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(contentHeight + 82.0f, 170.0f, 420.0f));
        PositionTooltip(iconRect, _perkPopupRect != null ? _perkPopupRect : _ringPopupRect);
        _tooltipRect.gameObject.SetActive(true);
    }

    public void OnPerkIconPointerEnter()
    {
        CancelHidePerkPopup();
        if (!_isEventVisible || _perkPopupObject == null)
        {
            return;
        }

        SyncPerkPopupLayoutFromRing();
        RefreshOwnedPerks();
        _perkPopupObject.SetActive(true);
    }

    public void OnPerkIconPointerExit()
    {
        SchedulePerkPopupHide();
    }

    public void OnPerkPopupPointerEnter()
    {
        CancelHidePerkPopup();
        CancelHideTooltip();
    }

    public void OnPerkPopupPointerExit()
    {
        SchedulePerkPopupHide();
    }
    #endregion

    #region 커스텀 (Private)
    private void SubscribeEvents()
    {
        _legacyShopManager = LegacyShopManager.Instance != null ? LegacyShopManager.Instance : _legacyShopManager;
        _gameManager = GameManager.Instance;
        _billManager = BillManager.Instance;
        _perkManager = PerkManager.Instance;

        if (_legacyShopManager != null)
        {
            _legacyShopManager.OnOwnershipDataChanged -= HandleOwnershipChanged;
            _legacyShopManager.OnOwnershipDataChanged += HandleOwnershipChanged;
        }

        if (_perkManager != null)
        {
            _perkManager.OnPerkStateChanged -= HandlePerkStateChanged;
            _perkManager.OnPerkStateChanged += HandlePerkStateChanged;
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

    private void HandlePerkStateChanged()
    {
        RefreshOwnedPerks();
        RefreshVisibility();
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
        bool hasPerks = HasOwnedPerk();
        if (_hammerIconObject != null)
        {
            _hammerIconObject.SetActive(_isEventVisible && _isHammerOwned);
        }

        if (_ringIconObject != null)
        {
            _ringIconObject.SetActive(_isEventVisible && hasRings);
        }

        if (_perkIconObject != null)
        {
            _perkIconObject.SetActive(_isEventVisible && hasPerks);
        }

        if (!_isEventVisible && _ringPopupObject != null)
        {
            _ringPopupObject.SetActive(false);
            if (_tooltipRect != null)
            {
                _tooltipRect.gameObject.SetActive(false);
            }
        }

        if (!_isEventVisible && _perkPopupObject != null)
        {
            _perkPopupObject.SetActive(false);
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

    private bool HasOwnedPerk()
    {
        if (_perkManager == null)
        {
            _perkManager = PerkManager.Instance;
        }

        return _perkManager != null && _perkManager.OwnedPerks != null && _perkManager.OwnedPerks.Count > 0;
    }

    private void PositionTooltip(RectTransform iconRect, RectTransform popupRoot)
    {
        if (iconRect == null || _canvas == null || _tooltipRect == null) return;
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

        RectTransform tooltipParent = _tooltipRect.parent as RectTransform;
        if (tooltipParent == null)
        {
            tooltipParent = popupRoot;
        }

        if (tooltipParent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParent, new Vector2(left, top), eventCamera, out Vector2 localPoint))
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

    private void SchedulePerkPopupHide()
    {
        CancelHidePerkPopup();
        _hidePerkPopupCoroutine = StartCoroutine(HidePerkPopupAfterDelay());
    }

    private IEnumerator HidePerkPopupAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.15f);

        if (_perkPopupObject != null)
        {
            _perkPopupObject.SetActive(false);
        }

        if (_tooltipRect != null)
        {
            _tooltipRect.gameObject.SetActive(false);
        }

        _hidePerkPopupCoroutine = null;
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

    private void CancelHidePerkPopup()
    {
        if (_hidePerkPopupCoroutine == null)
        {
            return;
        }

        StopCoroutine(_hidePerkPopupCoroutine);
        _hidePerkPopupCoroutine = null;
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

    private void RefreshOwnedPerks()
    {
        if (_perkManager == null)
        {
            _perkManager = PerkManager.Instance;
        }

        RectTransform gridRoot = GetPerkGridRoot();
        if (_perkManager == null || _perkPopupRect == null || gridRoot == null)
        {
            return;
        }

        EnsurePerkGridLayout(gridRoot);

        IReadOnlyList<PerkData> ownedPerks = _perkManager.OwnedPerks;
        UpdatePerkPopupHeight(ownedPerks.Count, gridRoot);
        EnsurePerkTileCount(ownedPerks.Count, gridRoot);
        for (int i = 0; i < _dynamicPerkTiles.Count; i++)
        {
            PerkHudTile tile = _dynamicPerkTiles[i];
            if (tile == null)
            {
                continue;
            }

            if (i < ownedPerks.Count)
            {
                tile.Configure(ownedPerks[i], this);
                tile.SetVisible(true);
            }
            else
            {
                tile.SetVisible(false);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_perkPopupRect);
    }

    private void UpdatePerkPopupHeight(int perkCount, RectTransform gridRoot)
    {
        if (_perkPopupRect == null || gridRoot == null)
        {
            return;
        }

        GridLayoutGroup grid = gridRoot.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            return;
        }

        int columns = Mathf.Max(1, grid.constraintCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt(perkCount / (float)columns));

        float requiredHeight = grid.padding.top + grid.padding.bottom +
                               (grid.cellSize.y * rows) +
                               (grid.spacing.y * Mathf.Max(0, rows - 1));

        float minHeight = 220.0f;
        float targetHeight = Mathf.Max(minHeight, requiredHeight);
        _perkPopupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private void EnsurePerkTileCount(int count, RectTransform gridRoot)
    {
        SyncExistingPerkTiles(gridRoot);

        while (_dynamicPerkTiles.Count < count)
        {
            GameObject tileObject = new GameObject("PerkTile_" + _dynamicPerkTiles.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PerkHudTile));
            tileObject.transform.SetParent(gridRoot, false);
            RectTransform rect = tileObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(84.0f, 84.0f);
            Image background = tileObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.16f, 0.16f, 0.85f);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(tileObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(62.0f, 62.0f);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.sprite = _perkDefaultIcon;

            PerkHudTile tile = tileObject.GetComponent<PerkHudTile>();
            tile.Configure(null, this);
            _dynamicPerkTiles.Add(tile);
        }
    }

    private void SyncExistingPerkTiles(RectTransform gridRoot)
    {
        if (gridRoot == null)
        {
            return;
        }

        _dynamicPerkTiles.RemoveAll(tile => tile == null || tile.transform == null || tile.transform.parent != gridRoot);

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gridRoot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            PerkHudTile tile = child.GetComponent<PerkHudTile>();
            if (tile == null)
            {
                Destroy(child.gameObject);
                continue;
            }

            tile.Configure(tile.Perk, this);
            if (!_dynamicPerkTiles.Contains(tile))
            {
                _dynamicPerkTiles.Add(tile);
            }
        }
    }

    private RectTransform GetPerkGridRoot()
    {
        if (_perkGridRect != null)
        {
            return _perkGridRect;
        }

        if (_perkPopupRect == null)
        {
            return null;
        }

        Transform gridTransform = _perkPopupRect.Find("PerkGrid");
        _perkGridRect = gridTransform != null ? gridTransform as RectTransform : _perkPopupRect;
        return _perkGridRect;
    }

    private void EnsurePerkGridLayout(RectTransform gridRoot)
    {
        GridLayoutGroup grid = gridRoot.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = new Vector2(84.0f, 84.0f);
        grid.spacing = new Vector2(8.0f, 8.0f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
    }

    private void EnsureTooltipParent(RectTransform popupRoot)
    {
        if (_tooltipRect == null || popupRoot == null)
        {
            return;
        }

        if (_tooltipRect.parent != popupRoot)
        {
            _tooltipRect.SetParent(popupRoot, false);
            _tooltipRect.SetAsLastSibling();
        }
    }

    private void SyncPerkPopupLayoutFromRing()
    {
        if (_perkPopupRect == null || _ringPopupRect == null)
        {
            return;
        }

        _perkPopupRect.anchorMin = _ringPopupRect.anchorMin;
        _perkPopupRect.anchorMax = _ringPopupRect.anchorMax;
        _perkPopupRect.pivot = _ringPopupRect.pivot;

        float iconDeltaX = 0.0f;
        RectTransform ringIconRect = _ringIconObject != null ? _ringIconObject.transform as RectTransform : null;
        RectTransform perkIconRect = _perkIconObject != null ? _perkIconObject.transform as RectTransform : null;
        if (ringIconRect != null && perkIconRect != null)
        {
            iconDeltaX = perkIconRect.anchoredPosition.x - ringIconRect.anchoredPosition.x;
        }

        Vector2 position = _ringPopupRect.anchoredPosition;
        position.x += iconDeltaX;
        _perkPopupRect.anchoredPosition = position;
    }
    #endregion
}
