/// <summary>
/// IPlayerAttackTarget을 구현한 대상에만 피해를 전달
/// </summary>
public interface IPlayerAttackTarget
{
    void ReceivePlayerAttack(float damage);
}
