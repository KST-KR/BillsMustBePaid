using UnityEngine;

/// <summary>
/// HandPivot의 Animation Event를 PlayerCombat에 전달
/// </summary>
public class HandAnimationEventRelay : MonoBehaviour
{
    #region 내부 변수
    private PlayerCombat _playerCombat;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _playerCombat = GetComponentInParent<PlayerCombat>();

        if (_playerCombat == null)
        {
            Debug.LogWarning("PlayerCombat를 찾을 수 없음.");
        }
    }
    #endregion

    /// <summary>
    /// Animation Event에서 호출
    /// 타격 애니메이션 완료 신호 전달
    /// </summary>
    public void OnHitAnimationCompleted()
    {
        _playerCombat?.OnHitAnimationCompleted();
    }

    /// <summary>
    /// Animation Event에서 호출
    /// 스태미나 고갈 연출 완료 신호 전달
    /// </summary>
    public void OnTiredAnimationCompleted()
    {
        _playerCombat?.NotifyTiredAnimationCompleted();
    }
}
