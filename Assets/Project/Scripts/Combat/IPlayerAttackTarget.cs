/// <summary>
/// IPlayerAttackTarget을 구현한 대상에만 피해를 전달
/// </summary>
public interface IPlayerAttackTarget
{
    /// <summary>
    /// 플레이어의 공격을 받았을 때 호출
    /// </summary>
    void TakePlayerAttack(float damage);
}
