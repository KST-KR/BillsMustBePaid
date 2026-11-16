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
    [SerializeField] private RectTransform _perkTooltipRect;
    [SerializeField] private TMP_Text _perkTooltipTitle;
    [SerializeField] private TMP_Text _perkTooltipDescription;
    #endregion

    #region 내부 변수
    private GameManager _gameManager;
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
        _perkGridRect = _perkPopupRect != null ? _perkPopupRect.Find("PerkGrid") as RectTransform : null;
        _perkTiles = perkTiles;
        _perkDefaultIcon = perkDefaultIcon;
        EnsurePerkPointerBindings();
        EnsurePerkTooltipBindings();
    }

    #region 이벤트 함수
    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        EnsurePerkPointerBindings();
        EnsurePerkTooltipBindings();
        SubscribeEvents();
        RefreshOwnedItems();
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
        RectTransform tooltipRect = _tooltipRect;
        TMP_Text tooltipTitle = _tooltipTitle;
        TMP_Text tooltipDescription = _tooltipDescription;

        if (!_isEventVisible || item == null || tooltipRect == null || tooltipTitle == null || tooltipDescription == null)
        {
            return;
        }

        EnsureTooltipParent(tooltipRect, _ringPopupRect);

        CancelHidePopup();
        CancelHideTooltip();

        tooltipTitle.text = item.DisplayName;
        tooltipDescription.text = item.Description + "\n\n" + item.StatEffectText;

        float scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1.0f;
        float width = Mathf.Min(CTooltipWidth, (Screen.width - CScreenEdgePadding * 2.0f) / scale);

        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        tooltipDescription.ForceMeshUpdate();
        float contentHeight = tooltipDescription.GetPreferredValues(tooltipDescription.text, tooltipDescription.rectTransform.rect.width, 0.0f).y;

        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(contentHeight + 82.0f, 170.0f, 420.0f));
        PositionTooltip(tooltipRect, iconRect, _ringPopupRect);
        tooltipRect.gameObject.SetActive(true);
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
        RectTransform tooltipRect = _perkTooltipRect != null ? _perkTooltipRect : _tooltipRect;
        TMP_Text tooltipTitle = _perkTooltipTitle != null ? _perkTooltipTitle : _tooltipTitle;
        TMP_Text tooltipDescription = _perkTooltipDescription != null ? _perkTooltipDescription : _tooltipDescription;

        if (perk == null || tooltipRect == null || tooltipTitle == null || tooltipDescription == null)
        {
            return;
        }

        if (!_isEventVisible && (_perkPopupObject == null || !_perkPopupObject.activeSelf))
        {
            return;
        }

        EnsureTooltipParent(tooltipRect, _perkPopupRect != null ? _perkPopupRect : _ringPopupRect);

        CancelHidePerkPopup();
        CancelHideTooltip();

        tooltipTitle.text = perk.DisplayName;
        tooltipDescription.text = perk.EffectText + "\n" + "TIER " + perk.Tier;

        float scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1.0f;
        float width = Mathf.Min(CTooltipWidth, (Screen.width - CScreenEdgePadding * 2.0f) / scale);
        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        tooltipDescription.ForceMeshUpdate();
        float contentHeight = tooltipDescription.GetPreferredValues(tooltipDescription.text, tooltipDescription.rectTransform.rect.width, 0.0f).y;

        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(contentHeight + 82.0f, 170.0f, 420.0f));
        PositionTooltip(tooltipRect, iconRect, _perkPopupRect != null ? _perkPopupRect : _ringPopupRect);
        tooltipRect.gameObject.SetActive(true);
    }

    public void OnPerkIconPointerEnter()
    {
        CancelHidePerkPopup();
        if (!_isEventVisible || _perkPopupObject == null)
        {
            return;
        }

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
        _isEventVisible = _gameManager != null && _gameManager.State == EGameState.StageClear;

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
            HideAllTooltips();
        }

        if (!_isEventVisible && _perkPopupObject != null)
        {
            _perkPopupObject.SetActive(false);
            HideAllTooltips();
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

    private void PositionTooltip(RectTransform tooltipRect, RectTransform iconRect, RectTransform popupRoot)
    {
        if (iconRect == null || _canvas == null || tooltipRect == null) return;
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
        float width = tooltipRect.rect.width * scale;
        float height = tooltipRect.rect.height * scale;
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

        RectTransform tooltipParent = tooltipRect.parent as RectTransform;
        if (tooltipParent == null)
        {
            tooltipParent = popupRoot;
        }

        if (tooltipParent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParent, new Vector2(left, top), eventCamera, out Vector2 localPoint))
        {
            tooltipRect.anchoredPosition = localPoint;
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

        HideAllTooltips();

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

        HideAllTooltips();

        _hidePerkPopupCoroutine = null;
    }

    private IEnumerator HideTooltipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.12f);

        HideAllTooltips();

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
        UpdatePerkPopupSize(ownedPerks.Count, gridRoot);
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

    private void UpdatePerkPopupSize(int perkCount, RectTransform gridRoot)
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

        int maxColumns = Mathf.Max(1, grid.constraintCount);
        int columns = Mathf.Clamp(perkCount, 1, maxColumns);
        int rows = Mathf.Max(1, Mathf.CeilToInt(perkCount / (float)maxColumns));

        float requiredWidth = grid.padding.left + grid.padding.right +
                              (grid.cellSize.x * columns) +
                              (grid.spacing.x * Mathf.Max(0, columns - 1));

        float requiredHeight = grid.padding.top + grid.padding.bottom +
                               (grid.cellSize.y * rows) +
                               (grid.spacing.y * Mathf.Max(0, rows - 1));

        _perkPopupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
        _perkPopupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
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

    private void EnsureTooltipParent(RectTransform tooltipRect, RectTransform popupRoot)
    {
        if (tooltipRect == null || popupRoot == null)
        {
            return;
        }

        if (tooltipRect.parent != popupRoot)
        {
            tooltipRect.SetParent(popupRoot, false);
            tooltipRect.SetAsLastSibling();
        }
    }

    private void HideAllTooltips()
    {
        if (_tooltipRect != null)
        {
            _tooltipRect.gameObject.SetActive(false);
        }

        if (_perkTooltipRect != null)
        {
            _perkTooltipRect.gameObject.SetActive(false);
        }
    }

    private void EnsurePerkPointerBindings()
    {
        if (_perkIconObject != null)
        {
            PerkHudPointer iconPointer = _perkIconObject.GetComponent<PerkHudPointer>();
            if (iconPointer == null)
            {
                iconPointer = _perkIconObject.AddComponent<PerkHudPointer>();
            }

            iconPointer.Configure(this, false);

            Image iconImage = _perkIconObject.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.raycastTarget = true;
            }
        }

        if (_perkPopupObject != null)
        {
            PerkHudPointer popupPointer = _perkPopupObject.GetComponent<PerkHudPointer>();
            if (popupPointer == null)
            {
                popupPointer = _perkPopupObject.AddComponent<PerkHudPointer>();
            }

            popupPointer.Configure(this, true);

            Image popupImage = _perkPopupObject.GetComponent<Image>();
            if (popupImage != null)
            {
                popupImage.raycastTarget = true;
            }
        }
    }

    private void EnsurePerkTooltipBindings()
    {
        if (_perkPopupRect == null)
        {
            return;
        }

        if (_perkTooltipRect == null)
        {
            Transform tooltipTransform = _perkPopupRect.Find("PerkStatTooltip");
            if (tooltipTransform != null)
            {
                _perkTooltipRect = tooltipTransform as RectTransform;
            }
        }

        if (_perkTooltipRect == null)
        {
            return;
        }

        if (_perkTooltipTitle == null)
        {
            Transform titleTransform = _perkTooltipRect.Find("Title");
            if (titleTransform != null)
            {
                _perkTooltipTitle = titleTransform.GetComponent<TMP_Text>();
            }
        }

        if (_perkTooltipDescription == null)
        {
            Transform descriptionTransform = _perkTooltipRect.Find("Description");
            if (descriptionTransform != null)
            {
                _perkTooltipDescription = descriptionTransform.GetComponent<TMP_Text>();
            }
        }
    }
    #endregion
}
