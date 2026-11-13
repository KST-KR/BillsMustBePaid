using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 퍼크 타일 표시
/// </summary>
public sealed class PerkHudTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private PerkData _perk;
    [SerializeField] private Image _icon;
    [SerializeField] private LegacyRelicHud _hud;

    public PerkData Perk => _perk;

    public void Configure(PerkData perk, LegacyRelicHud hud)
    {
        _perk = perk;
        _hud = hud;
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.sprite = null;
            rootImage.color = new Color(0.16f, 0.16f, 0.16f, 0.85f);
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = false;
            rootImage.raycastTarget = true;
        }
        if (_icon == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                _icon = iconTransform.GetComponent<Image>();
            }
        }
        if (_icon != null)
        {
            _icon.sprite = perk != null ? perk.Icon : null;
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _icon.color = perk != null ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hud != null && _perk != null)
        {
            _hud.ShowPerkTooltip(_perk, transform as RectTransform);
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
