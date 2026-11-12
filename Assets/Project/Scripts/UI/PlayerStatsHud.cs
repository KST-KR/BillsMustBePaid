using UnityEngine;

/// <summary>
/// 플레이어 스테이터스 아이콘 표시 관리
/// </summary>
public class PlayerStatsHud : MonoBehaviour
{
    [Header("스테이터스 아이콘")]
    [SerializeField] private GameObject _hammerIcon;

    [Header("스테이터스 팝업")]
    [SerializeField] private PlayerStatsUI _playerStatsUI;

    public void SetVisible(bool isVisible)
    {
        if (_hammerIcon != null)
        {
            _hammerIcon.SetActive(isVisible);
        }

        if (!isVisible && _playerStatsUI != null)
        {
            _playerStatsUI.Hide();
        }
    }
}