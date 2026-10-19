using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마우스 입력만 처리
/// 실제 이동속도나 위치 변경은 담당하지 않음.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    #region 인스펙터
    [Header("입력 설정")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _groundLayer;
    #endregion

    #region 프로퍼티
    public Vector3 MouseWorldPosition { get; private set; }
    public bool HasMouseWorldPosition { get; private set; }
    #endregion

    #region 이벤트
    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    void Update()
    {
        UpdateMouseWorldPosition();
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 마우스 입력 값에 따라 업데이트
    /// </summary>
    private void UpdateMouseWorldPosition()
    {
        HasMouseWorldPosition = false;

        if (_mainCamera == null)
        {
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer))
        {
            MouseWorldPosition = hit.point;
            HasMouseWorldPosition = true;
        }
    }
    #endregion

}
