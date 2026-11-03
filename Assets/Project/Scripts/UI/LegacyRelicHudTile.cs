using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 반지 타일의 표시와 포인터 이벤트를 연결
/// </summary>
public sealed class LegacyRelicHudTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private LegacyShopItemData _item;
    [SerializeField] private Image _icon;
    [SerializeField] private LegacyRelicHud _hud;

    public LegacyShopItemData Item => _item;

    public void Configure(LegacyShopItemData item, Sprite icon, LegacyRelicHud hud)
    {
        _item = item;
        _hud = hud;
        if (_icon == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                _icon = iconTransform.GetComponent<Image>();
            }
        }
        if (_icon != null) _icon.sprite = icon;
    }

    public void SetOwned(bool owned)
    {
        gameObject.SetActive(owned);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hud != null)
        {
            _hud.ShowItemTooltip(_item, transform as RectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_hud != null)
        {
            _hud.ScheduleTooltipHide();
        }
    }
}
