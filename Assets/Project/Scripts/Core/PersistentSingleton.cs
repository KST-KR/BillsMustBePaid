/// <summary>
/// 씬이 바뀌어도 파괴되지 않는 싱글톤 기반 클래스
/// 예: 타이틀 씬과 게임 씬에서 함께 쓰는 저장, 설정, 오디오 매니저
/// </summary>
public abstract class PersistentSingleton<T> : Singleton<T> where T : PersistentSingleton<T>
{
    protected override bool PersistAcrossScenes => true;
}