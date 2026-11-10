public interface IHandState
{
    // 해당 상태에 처음 진입했을 떄 한번 실행
    void Enter();

    // 해당 상태가 유지되는 동안 매 프레임 실행
    void Update();

    // 다른 상태로 전환되기 직전에 한번 실행
    void Exit();
}