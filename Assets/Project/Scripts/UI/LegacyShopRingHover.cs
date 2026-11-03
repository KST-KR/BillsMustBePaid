using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 레거시 상점 링의 마우스 오버 강조와 설명 패널 표시 및 구매요청 관리를 처리하니다.
/// </summary>
public sealed class LegacyShopRingHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private GameObject _tooltipRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _ringLabelText;
    [SerializeField] private TMP_Text _balanceText;
    [SerializeField] private LegacyShopPurchaseConfirmPopup _purchaseConfirmPopup;
    [SerializeField] private LegacyShopItemData _itemData;
    [SerializeField] private Camera _hoverCamera;

    private Renderer[] _renderers;
    private MaterialPropertyBlock[] _originalPropertyBlocks;
    private bool _hovering;
    private bool _isOwned;
    private bool _initialized;
    private LegacyShopManager _legacyShopManager;

    public void Initialize(GameObject tooltipRoot, TMP_Text titleText, TMP_Text descriptionText,
        TMP_Text priceText, TMP_Text ringLabelText, TMP_Text balanceText, LegacyShopPurchaseConfirmPopup purchaseConfirmPopup,
        Camera hoverCamera, LegacyShopItemData itemData)
    {
        _tooltipRoot = tooltipRoot;
        _titleText = titleText;
        _descriptionText = descriptionText;
        _priceText = priceText;
        _ringLabelText = ringLabelText;
        _balanceText = balanceText;
        _purchaseConfirmPopup = purchaseConfirmPopup;
        _itemData = itemData;
        _hoverCamera = hoverCamera;
        SyncItemData();
        if (_itemData == null)
        {
            Debug.LogError("Legacy Shop SO 누락", this);
            return;
        }
        CacheRenderers();
        RefreshOwnedAppearance();
    }

    private void Awake()
    {
        SyncItemData();
        CacheRenderers();
        RefreshOwnedAppearance();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        _legacyShopManager = LegacyShopManager.GetOrCreate();
        _legacyShopManager.OnOwnershipDataChanged += HandleOwnershipDataChanged;
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
        }
        SyncItemData();
        RefreshOwnedAppearance();
        RefreshBalanceText();
    }

    private void OnDestroy()
    {
        if (_legacyShopManager != null)
        {
            _legacyShopManager.OnOwnershipDataChanged -= HandleOwnershipDataChanged;
        }
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
        }
    }

    private void OnMouseEnter()
    {
        ShowTooltip();
    }

    private void OnMouseExit()
    {
        HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RequestPurchase();
    }

    private void ShowTooltip()
    {
        if (_purchaseConfirmPopup != null && _purchaseConfirmPopup.IsOpen)
        {
            return;
        }
        if (!EnsureInitialized())
        {
            return;
        }

        SyncItemData();
        _hovering = true;
        SetHoverVisual(true);
        if (_titleText != null) _titleText.text = _itemData.DisplayName;
        if (_descriptionText != null) _descriptionText.text = _itemData.Description + "\n" + _itemData.StatEffectText;
        UpdatePriceText();
        if (_tooltipRoot != null) _tooltipRoot.SetActive(true);
    }

    private void HideTooltip()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        _hovering = false;
        SetHoverVisual(false);
        if (_tooltipRoot != null) _tooltipRoot.SetActive(false);
    }

    private void RequestPurchase()
    {
        if (_purchaseConfirmPopup != null && _purchaseConfirmPopup.IsOpen)
        {
            return;
        }
        if (!EnsureInitialized()) return;
        if (_isOwned)
        {
            if (_priceText != null) _priceText.text = "보유 중 · 장착됨";
            return;
        }

        if (_purchaseConfirmPopup != null)
        {
            HideTooltip();
            _purchaseConfirmPopup.Open(this, _itemData.DisplayName,
                _itemData.Description + "\n" + _itemData.StatEffectText, _itemData.FormattedPrice);
        }
        else if (_priceText != null)
        {
            _priceText.text = "구매 확인 창이 연결되지 않았습니다";
        }
    }

    public bool TryPurchase(out string result)
    {
        result = string.Empty;
        if (!EnsureInitialized())
        {
            result = "아이템 정보를 불러오지 못했습니다";
            return false;
        }
        if (_isOwned)
        {
            result = "이미 보유한 아이템입니다";
            return false;
        }

        int cost = _itemData.Price;
        LegacyShopManager manager = LegacyShopManager.GetOrCreate();
        if (!manager.TryPurchase(_itemData))
        {
            result = CurrencyManager.Instance == null
                ? "재화 매니저를 찾을 수 없습니다"
                : CurrencyManager.Instance.LegacyPoints < cost ? "포인트가 부족합니다" : "구매 처리에 실패했습니다";
            return false;
        }

        _isOwned = true;
        UpdatePriceText();
        RefreshOwnedAppearance();
        Debug.Log("레거시 링을 구매했습니다: " + _itemData.DisplayName + " / " + cost + " 포인트", this);
        result = "구매가 완료되었습니다";
        return true;
    }

    private void UpdatePriceText()
    {
        if (_priceText == null) return;
        _priceText.text = _isOwned
            ? "보유 중 · 장착됨"
            : "구매 필요 · " + _itemData.FormattedPrice + " 포인트";
    }

    private void RefreshOwnedAppearance()
    {
        SyncItemData();
        if (_itemData == null) return;
        if (_ringLabelText != null)
        {
            _ringLabelText.text = _isOwned ? "장착됨" : _itemData.FormattedPrice + " ◈";
            _ringLabelText.color = _isOwned
                ? new Color(0.84f, 0.67f, 0.44f)
                : new Color(0.78f, 0.69f, 0.58f);
        }

        if (_renderers == null) return;
        Material stateMaterial = _isOwned ? _itemData.ActiveMaterial : _itemData.LockedMaterial;
        if (stateMaterial == null)
        {
            Debug.LogWarning("아이템 SO의 활성/잠금 재질 참조를 확인하세요: " + _itemData.name, _itemData);
            return;
        }

        for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
        {
            Renderer target = _renderers[rendererIndex];
            if (target == null) continue;

            Material[] materials = target.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                materials[materialIndex] = stateMaterial;
            }
            target.sharedMaterials = materials;
        }
    }

    public void RefreshAppearanceFromData()
    {
        SyncItemData();
        CacheRenderers();
        RefreshOwnedAppearance();
    }

    private void SyncItemData()
    {
        if (_itemData == null) return;

        if (Application.isPlaying)
        {
            _legacyShopManager = LegacyShopManager.Instance != null
                ? LegacyShopManager.Instance
                : LegacyShopManager.GetOrCreate();
            _isOwned = _legacyShopManager.IsOwned(_itemData);
            return;
        }

        _isOwned = _itemData.InitiallyOwned;
    }

    private void HandleOwnershipDataChanged()
    {
        SyncItemData();
        RefreshOwnedAppearance();
        UpdatePriceText();
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        if (type == ECurrencyType.LegacyPoint)
        {
            RefreshBalanceText();
        }
    }

    private void RefreshBalanceText()
    {
        if (_balanceText != null && CurrencyManager.Instance != null)
        {
            _balanceText.text = FormatBalance(CurrencyManager.Instance.LegacyPoints);
        }
    }

    private static string FormatBalance(int balance)
    {
        return balance >= 10000
            ? (balance / 10000.0f).ToString("0.##") + "만"
            : balance.ToString("N0");
    }

    private void CacheRenderers()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalPropertyBlocks = new MaterialPropertyBlock[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalPropertyBlocks[i] = new MaterialPropertyBlock();
                _renderers[i].GetPropertyBlock(_originalPropertyBlocks[i]);
            }
        }
        if (_hoverCamera == null)
        {
            _hoverCamera = Camera.main;
        }
        _initialized = _itemData != null && _tooltipRoot != null && _titleText != null &&
            _descriptionText != null && _priceText != null && _ringLabelText != null;
    }

    private bool EnsureInitialized()
    {
        if (!_initialized)
        {
            CacheRenderers();
        }
        return _initialized;
    }

    private void SetHoverVisual(bool hovering)
    {
        if (_renderers == null || _originalPropertyBlocks == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer target = _renderers[i];
            if (target == null)
            {
                continue;
            }

            if (!hovering)
            {
                target.SetPropertyBlock(_originalPropertyBlocks[i]);
                continue;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            Material material = target.sharedMaterial;
            if (material != null)
            {
                if (material.HasProperty("_Color")) block.SetColor("_Color", Color.Lerp(material.color, Color.white, 0.25f));
                if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", Color.Lerp(material.color, Color.white, 0.25f));
            }
            target.SetPropertyBlock(block);
        }
    }
}
