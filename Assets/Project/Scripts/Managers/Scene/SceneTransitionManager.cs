using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 관리
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    #region 인스펙터
    [Header("씬 설정")]
    [SerializeField] private string _gameSceneName = "Game";
    #endregion

    #region 프로퍼티
    public static SceneTransitionManager Instance { get; private set; }
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void LoadGameScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
        {
            Debug.LogError($"게임 씬을 찾을 수 없음: {_gameSceneName}");
            return;
        }

        SceneManager.LoadScene(_gameSceneName);
    }
}
