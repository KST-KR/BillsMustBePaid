using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레거시 상점 아이템 레이블 카메라 고정
/// </summary>
public sealed class LegacyShopProjectedLabel : MonoBehaviour
{
    [SerializeField] private Transform _ringTransform;
    [SerializeField] private Camera _camera;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Vector2 _screenOffset = new Vector2(0.0f, -34.0f);

    private RectTransform _labelRect;
    private RectTransform _canvasRect;
    private CanvasGroup _canvasGroup;
    private RectTransform _parentRect;

    public void Initialize(Transform ringTransform, Camera targetCamera, Canvas targetCanvas, Vector2 screenOffset)
    {
        _ringTransform = ringTransform;
        _camera = targetCamera;
        _canvas = targetCanvas;
        _screenOffset = screenOffset;
        _labelRect = transform as RectTransform;
        _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
        _parentRect = _labelRect != null ? _labelRect.parent as RectTransform : null;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        RefreshPosition();
    }

    private void LateUpdate()
    {
        RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (_ringTransform == null || _camera == null || _canvas == null || _labelRect == null || _canvasRect == null || _parentRect == null)
        {
            return;
        }

        Vector3 ringScreenPosition = _camera.WorldToScreenPoint(_ringTransform.position);
        bool visible = ringScreenPosition.z > _camera.nearClipPlane && ringScreenPosition.z < _camera.farClipPlane;
        _canvasGroup.alpha = visible ? 1.0f : 0.0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;

        if (!visible)
        {
            return;
        }

        Vector2 labelScreenPosition = new Vector2(ringScreenPosition.x, ringScreenPosition.y) + _screenOffset;
        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, labelScreenPosition, uiCamera, out Vector2 localPosition))
        {
            _labelRect.anchoredPosition = localPosition;
        }
    }
}
