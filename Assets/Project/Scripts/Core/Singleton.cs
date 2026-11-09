using UnityEngine;

/// <summary>
/// MonoBehaviour 싱글톤의 공통 기반 클래스 (씬이 바뀌면 같이 사라짐)
/// 씬을 넘어 유지해야 하는 매니저는 PersistentSingleton을 상속
/// </summary>
[DisallowMultipleComponent]
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    #region 내부 변수
    private static T _instance;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 등록된 인스턴스
    /// 아직 없거나 파괴되었으면 null
    /// </summary>
    public static T Instance => _instance != null ? _instance : null;

    /// <summary>
    /// 사용할 수 있는 인스턴스가 있는지
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// 이 객체가 등록된 인스턴스인지 (중복 인스턴스가 아닌지)
    /// </summary>
    protected bool IsActiveInstance => _instance == this;

    /// <summary>
    /// true이면 씬이 바뀌어도 파괴되지 않음 (PersistentSingleton에서 재정의)
    /// </summary>
    protected virtual bool PersistAcrossScenes => false;
    #endregion

    #region 이벤트 함수
    /// <summary>
    /// 인스턴스를 등록하고 중복이면 스스로 제거
    /// 파생 클래스에서 Awake를 직접 선언하면 이 등록이 실행되지 않음
    /// 초기화는 OnSingletonAwake에 작성 (직접 선언하면 컴파일러가 "숨김" 경고를 냄)
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 이미 인스턴스가 있어 중복 인스턴스가 있어 중복 오브젝트를 제거: {name}");

            // Destroy는 프레임 끝에 처리되므로 그 전에 OnEnable / Start / Update가 실행되지 않도록 비활성화
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;

        if (PersistAcrossScenes)
        {
            // DontDestroyOnLoad는 루트 오브젝트에서만 동작하므로 루트를 대상으로 함.
            DontDestroyOnLoad(gameObject);
        }

        OnSingletonAwake();
    }

    /// <summary>
    /// 등록된 인스턴스가 파괴될 때만 정리 (중복 인스턴스의 파괴는 무시)
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        OnSingletonDestroy();

        _instance = null;
    }
    #endregion

    /// <summary>
    /// 등록된 인스턴스에서만 Awake 시점에 한번 호출 (중복 인스턴스에서는 호출되지 않음)
    /// </summary>
    protected virtual void OnSingletonAwake()
    {

    }

    /// <summary>
    /// 등록된 인스턴스가 파괴될 때 호출 (Instance는 아직 이 객체를 가리킴)
    /// </summary>
    protected virtual void OnSingletonDestroy()
    {

    }
}
