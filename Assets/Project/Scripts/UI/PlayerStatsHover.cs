using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 망치 아이콘의 마우스 Hover 감지
/// </summary>
public class PlayerStatsHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region 인스펙터
    [SerializeField] private PlayerStatsUI _playerStatsUI;
    #endregion

    #region 이벤트 시스템
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_playerStatsUI == null)
        {
            return;
        }

        RectTransform anchor = transform as RectTransform;

        if (anchor == null)
        {
            return;
        }

        _playerStatsUI.Show(anchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_playerStatsUI == null)
        {
            return;
        }

        _playerStatsUI.Hide();
    }
    #endregion

}