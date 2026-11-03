using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ShopRawImage에서 마우스 위치를 감지하고
/// ShopCamera의 Raycast로 진열된 망치를 찾기
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ShopDisplayInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    #region 인스펙터
    [Header("상점 카메라")]
    [SerializeField] private Camera _shopCamera;

    [Header("감지 대상")]
    [SerializeField] private LayerMask _shopItemLayers = ~0;
    #endregion

    #region 내부 변수
    private RectTransform _rawImageRect;
    private ShopItem _currentShopItem;
    #endregion

    #region 이벤트
    /// 마우스가 새로운 망치를 가리킬 때 발생
    public event Action<ShopItem> OnShopItemHovered;

    // 마우스가 망치에서 벗어났을 때 발생
    public event Action OnShopItemHoverExited;
    #endregion

    #region 프로퍼티
    public ShopItem CurrentShopItem => _currentShopItem;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _rawImageRect = GetComponent<RectTransform>();

        if (_shopCamera == null)
        {
            Debug.LogError("ShopDisplayInput: ShopCamera가 할당되지 않음.", this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHoveredItem(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateHoveredItem(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearHoveredItem();
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// RawImage 안의 마우스 위치로 망치를 감지
    /// </summary>
    private void UpdateHoveredItem(PointerEventData eventData)
    {
        if (_shopCamera == null || _rawImageRect == null)
        {
            return;
        }

        // 화면 좌표를 RawImage 내부 좌표로 변환
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImageRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            ClearHoveredItem();
            return;
        }

        Rect rect = _rawImageRect.rect;

        // RawImage 내부 좌표를 Viewport 좌표(0~1)로 변환
        float viewportX = (localPoint.x - rect.xMin) / rect.width;
        float viewportY = (localPoint.y - rect.yMin) / rect.height;

        if (viewportX < 0f || viewportX > 1f ||
            viewportY < 0f || viewportY > 1f)
        {
            ClearHoveredItem();
            return;
        }

        // ShopCamera에서 해당 위치로 Ray 발사
        Ray ray = _shopCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, _shopCamera.farClipPlane,  _shopItemLayers))
        {
            ShopItem shopItem = hit.collider.GetComponentInParent<ShopItem>();

            SetHoveredItem(shopItem);
        }
        else
        {
            ClearHoveredItem();
        }
    }

    /// <summary>
    /// 현재 가리키는 망치 변경
    /// </summary>
    private void SetHoveredItem(ShopItem shopItem)
    {
        if (_currentShopItem == shopItem)
        {
            return;
        }

        _currentShopItem = shopItem;

        if (_currentShopItem != null)
        {
            OnShopItemHovered?.Invoke(_currentShopItem);
        }
        else
        {
            OnShopItemHoverExited?.Invoke();
        }
    }

    /// <summary>
    /// 현재 가리키는 망치 초기화
    /// </summary>
    private void ClearHoveredItem()
    {
        if (_currentShopItem == null)
        {
            return;
        }

        _currentShopItem = null;
        OnShopItemHoverExited?.Invoke();
    }
    #endregion
}
