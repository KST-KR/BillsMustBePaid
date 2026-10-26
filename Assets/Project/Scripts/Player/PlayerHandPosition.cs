using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PL-MOVE-02] 손의 위치를 마우스 좌표에 일치시킴
/// </summary>
public class PlayerHandPosition : MonoBehaviour
{
    #region 인스펙터
    [Header("입력 처리")]
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("손 위치 설정")]
    [SerializeField] private float _handHeight = 10.0f;
    #endregion

    #region 내부 변수
    private PlayerStageActivator _activator;
    private PlayerStamina _playerStamina;
    #endregion

    #region 프로퍼티
    private bool CanAct => _activator != null && _activator.CanAct && _playerStamina != null && !_playerStamina.IsExhausted;
    #endregion

    #region 이벤트
    private void Awake()
    {
        _activator = GetComponent<PlayerStageActivator>();
        _playerStamina = GetComponent<PlayerStamina>();
    }

    private void LateUpdate()
    {
        if (!CanAct)
        {
            return;
        }

        UpdateHandPosition();
    }
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 마우스 위치의 XZ 좌표와 설정한 높이로 손을 이동
    /// </summary>
    private void UpdateHandPosition()
    {
        if (_inputHandler == null)
        {
            return;
        }

        if (!_inputHandler.HasMouseWorldPosition)
        {
            return;
        }

        Vector3 mousePosition = _inputHandler.MouseWorldPosition;

        transform.position = new Vector3(mousePosition.x, _handHeight, mousePosition.z);
    }
    #endregion

}
