using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 관리
/// </summary>
public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    #region 인스펙터
    [Header("씬 설정")]
    [SerializeField] private string _gameSceneName = "Game";
    [SerializeField] private string _legacyShopSceneName = "GameLegacyShop";
    #endregion

    #region 커스텀 (public)
    public void LoadGameScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
        {
            Debug.LogError($"게임 씬을 찾을 수 없음: {_gameSceneName}");
            return;
        }

        SceneManager.LoadScene(_gameSceneName);
    }

    /// <summary>
    /// [SCENE-01] 파산 후 영구 구매 단계로 이동
    /// </summary>
    public bool LoadLegacyShopScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_legacyShopSceneName))
        {
            Debug.LogError($"레거시 상점 씬 Not Found: {_legacyShopSceneName}");
            return false;
        }

        SceneManager.LoadScene(_legacyShopSceneName);
        return true;
    }
    #endregion

}
