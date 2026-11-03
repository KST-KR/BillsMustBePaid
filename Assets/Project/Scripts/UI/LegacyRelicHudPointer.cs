using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 팝업 배경의 포인터 이벤트를 HUD에 전달
/// </summary>
public sealed class LegacyRelicHudPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
            _hud.OnPopupPointerEnter();
        }
        else
        {
            _hud.OnRingIconPointerEnter();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_hud == null) return;
        if (_isPopupSurface)
        {
            _hud.OnPopupPointerExit();
        }
        else
        {
            _hud.OnRingIconPointerExit();
        }
    }
}
