using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 퍼크 아이콘과 팝업 포인터 이벤트
/// </summary>
public sealed class PerkHudPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private LegacyRelicHud _hud;
    [SerializeField] private bool _isPopupSurface;

    public void Configure(LegacyRelicHud hud, bool isPopupSurface)
    {
        _hud = hud;
        _isPopupSurface = isPopupSurface;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hud == null)
        {
            return;
        }

        if (_isPopupSurface)
        {
            _hud.OnPerkPopupPointerEnter();
        }
        else
        {
            _hud.OnPerkIconPointerEnter();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_hud == null)
        {
            return;
        }

        if (_isPopupSurface)
        {
            _hud.OnPerkPopupPointerExit();
        }
        else
        {
            _hud.OnPerkIconPointerExit();
        }
    }
}
