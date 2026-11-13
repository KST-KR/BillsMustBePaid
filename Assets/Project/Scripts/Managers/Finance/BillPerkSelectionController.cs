using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 고지서 납부 후 퍼크 선택 UI 관리
/// </summary>
public sealed class BillPerkSelectionController : MonoBehaviour
{
    private const float CDimAlpha = 0.86f;

    #region 인스펙터
    [Header("UI Root")]
    [SerializeField] private GameObject _overlayRoot;
    [SerializeField] private RectTransform _cardContainer;

    [Header("Visual")]
    [SerializeField] private Sprite _cardBackgroundSprite;
    [SerializeField] private Sprite _cardShadowSprite;
    [SerializeField] private TMP_FontAsset _fontAsset;
    #endregion

    #region 내부 변수
    private readonly List<GameObject> _spawnedCards = new List<GameObject>();
    private readonly List<GameObject> _disabledBackgroundObjects = new List<GameObject>();
    private PerkManager _perkManager;
    private bool _isSubscribed;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        EnsureView();
        SetOverlayVisible(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        SetOverlayVisible(false);
    }
    #endregion

    #region 커스텀 Public
    public void Configure(Sprite cardBackgroundSprite, Sprite cardShadowSprite, TMP_FontAsset fontAsset)
    {
        _cardBackgroundSprite = cardBackgroundSprite;
        _cardShadowSprite = cardShadowSprite;
        _fontAsset = fontAsset;
        EnsureView();
    }
    #endregion

    #region 커스텀 Private
    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        _perkManager = PerkManager.Instance;
        if (_perkManager != null)
        {
            _perkManager.OnPerkOfferStarted += HandlePerkOfferStarted;
            _perkManager.OnPerkSelected += HandlePerkSelected;
        }

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_perkManager != null)
        {
            _perkManager.OnPerkOfferStarted -= HandlePerkOfferStarted;
            _perkManager.OnPerkSelected -= HandlePerkSelected;
        }

        _isSubscribed = false;
    }

    private void HandlePerkOfferStarted(IReadOnlyList<PerkData> perks, BillRecord bill)
    {
        EnsureView();
        RebuildCards(perks);

        SetOverlayVisible(true);
    }

    private void HandlePerkSelected(PerkData perk)
    {
        SetOverlayVisible(false);
    }

    private void SetOverlayVisible(bool isVisible)
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(isVisible);
            if (isVisible)
            {
                _overlayRoot.transform.SetAsLastSibling();
            }
        }

        SetBackgroundObjectsVisible(!isVisible);
    }

    private void RebuildCards(IReadOnlyList<PerkData> perks)
    {
        ClearCards();
        if (_cardContainer == null || perks == null)
        {
            return;
        }

        for (int i = 0; i < perks.Count; i++)
        {
            PerkData perk = perks[i];
            if (perk == null)
            {
                continue;
            }

            GameObject card = CreateCard(perk);
            _spawnedCards.Add(card);
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
            {
                Destroy(_spawnedCards[i]);
            }
        }

        _spawnedCards.Clear();

        if (_cardContainer == null)
        {
            return;
        }

        for (int i = _cardContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = _cardContainer.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private GameObject CreateCard(PerkData perk)
    {
        GameObject cardRoot = new GameObject("PerkCard_" + perk.PerkId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cardRoot.transform.SetParent(_cardContainer, false);
        RectTransform cardRect = cardRoot.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(340.0f, 474.0f);

        Image cardImage = cardRoot.GetComponent<Image>();
        if (_cardBackgroundSprite != null)
        {
            cardImage.sprite = _cardBackgroundSprite;
            cardImage.type = Image.Type.Sliced;
        }
        cardImage.color = new Color(1.0f, 1.0f, 1.0f, 0.98f);
        Outline cardOutline = cardRoot.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1.0f, 0.83f, 0.42f, 0.95f);
        cardOutline.effectDistance = new Vector2(2.0f, -2.0f);
        Shadow cardGlow = cardRoot.AddComponent<Shadow>();
        cardGlow.effectColor = new Color(1.0f, 0.66f, 0.20f, 0.38f);
        cardGlow.effectDistance = new Vector2(0.0f, -10.0f);

        Button button = cardRoot.GetComponent<Button>();
        string perkId = perk.PerkId;
        button.onClick.AddListener(() =>
        {
            if (_perkManager == null)
            {
                _perkManager = PerkManager.Instance;
            }
            if (_perkManager != null)
            {
                _perkManager.TrySelectPerk(perkId);
            }
        });

        GameObject shadowObject = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadowObject.transform.SetParent(cardRoot.transform, false);
        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(354.0f, 490.0f);
        shadowRect.anchoredPosition = new Vector2(0.0f, -10.0f);
        Image shadowImage = shadowObject.GetComponent<Image>();
        shadowImage.sprite = null;
        shadowImage.color = new Color(0.0f, 0.0f, 0.0f, 0.42f);
        shadowImage.raycastTarget = false;
        shadowObject.transform.SetAsFirstSibling();

        CreateIcon(cardRoot.transform, perk.Icon);
        CreateText(cardRoot.transform, "Name", perk.DisplayName, new Vector2(0.0f, -164.0f), 34.0f, FontStyles.Bold, new Color(1.0f, 1.0f, 1.0f, 1.0f), 292.0f, 54.0f, true);
        CreateText(cardRoot.transform, "Tier", "TIER " + perk.Tier, new Vector2(0.0f, -216.0f), 30.0f, FontStyles.Bold, new Color(1.0f, 0.84f, 0.50f, 1.0f), 292.0f, 44.0f, false);
        CreateText(cardRoot.transform, "Effect", perk.EffectText, new Vector2(0.0f, -304.0f), 30.0f, FontStyles.Bold, new Color(0.95f, 0.97f, 1.0f, 1.0f), 292.0f, 112.0f, true);

        return cardRoot;
    }

    private void CreateIcon(Transform parent, Sprite icon)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(parent, false);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1.0f);
        rect.anchorMax = new Vector2(0.5f, 1.0f);
        rect.pivot = new Vector2(0.5f, 1.0f);
        rect.sizeDelta = new Vector2(108.0f, 108.0f);
        rect.anchoredPosition = new Vector2(0.0f, -30.0f);
        Image image = iconObject.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = icon != null ? Color.white : new Color(1.0f, 1.0f, 1.0f, 0.3f);
    }

    private void CreateText(Transform parent, string objectName, string value, Vector2 anchoredPosition, float fontSize, FontStyles style, Color color, float width, float height, bool enableAutoSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1.0f);
        rect.anchorMax = new Vector2(0.5f, 1.0f);
        rect.pivot = new Vector2(0.5f, 1.0f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = anchoredPosition;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.color = color;
        text.enableAutoSizing = enableAutoSize;
        if (enableAutoSize)
        {
            text.fontSizeMin = Mathf.Max(16.0f, fontSize - 8.0f);
            text.fontSizeMax = fontSize;
        }

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.0f, 0.0f, 0.0f, 0.72f);
        outline.effectDistance = new Vector2(1.0f, -1.0f);
        if (_fontAsset != null)
        {
            text.font = _fontAsset;
        }
    }

    private void EnsureView()
    {
        if (_overlayRoot != null && _cardContainer != null)
        {
            return;
        }

        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
        {
            return;
        }

        if (_overlayRoot == null)
        {
            _overlayRoot = new GameObject("PerkSelectionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _overlayRoot.transform.SetParent(transform, false);
            RectTransform overlayRect = _overlayRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            Image dim = _overlayRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, CDimAlpha);
            dim.raycastTarget = true;
        }
        else
        {
            Image dim = _overlayRoot.GetComponent<Image>();
            if (dim != null)
            {
                dim.color = new Color(0f, 0f, 0f, CDimAlpha);
            }
        }

        RemoveLegacyHeaderText();

        if (_cardContainer == null)
        {
            GameObject containerObject = new GameObject("CardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerObject.transform.SetParent(_overlayRoot.transform, false);
            _cardContainer = containerObject.GetComponent<RectTransform>();
            _cardContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _cardContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _cardContainer.pivot = new Vector2(0.5f, 0.5f);
            _cardContainer.sizeDelta = new Vector2(1280.0f, 520.0f);
            _cardContainer.anchoredPosition = new Vector2(0.0f, -18.0f);
            HorizontalLayoutGroup layout = containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 44.0f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
        }
    }

    private void RemoveLegacyHeaderText()
    {
        RemoveChildIfExists("Title");
        RemoveChildIfExists("Notice");
    }

    private void RemoveChildIfExists(string childName)
    {
        if (_overlayRoot == null)
        {
            return;
        }

        Transform child = _overlayRoot.transform.Find(childName);
        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }

    private void SetBackgroundObjectsVisible(bool isVisible)
    {
        if (isVisible)
        {
            for (int i = 0; i < _disabledBackgroundObjects.Count; i++)
            {
                if (_disabledBackgroundObjects[i] != null)
                {
                    _disabledBackgroundObjects[i].SetActive(true);
                }
            }

            _disabledBackgroundObjects.Clear();
            return;
        }

        _disabledBackgroundObjects.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || (_overlayRoot != null && child.gameObject == _overlayRoot))
            {
                continue;
            }

            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                _disabledBackgroundObjects.Add(child.gameObject);
            }
        }
    }
    #endregion
}
