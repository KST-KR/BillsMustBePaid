using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 관리
/// </summary>
public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    #region 인스펙터
    [Header("씬 설정")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _gameSceneName = "Game";
    [SerializeField] private string _legacyShopSceneName = "GameLegacyShop";

    [Header("페이드 설정")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private float _fadeDuration = 1f;
    #endregion

    #region 내부 변수
    private bool _isTransitioning;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        if (_fadeCanvasGroup == null)
        {
            Debug.LogError("씬 전환 페이드 CanvasGroup이 연결되지 않음.");
            return;
        }

        // 씬 전환이 시작되기 전에는 페이드 화면을 숨김.
        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.interactable = false;
        _fadeCanvasGroup.blocksRaycasts = false;
    }
    #endregion

    #region 커스텀 (public)
    /// <summary>
    /// 메인 메뉴 씬으로 이동
    /// </summary>
    public bool LoadMainMenuScene()
    {
        return TryTransitionToScene(_mainMenuSceneName);
    }

    public void LoadGameScene()
    {
        TryTransitionToScene(_gameSceneName);
    }

    /// <summary>
    /// [SCENE-01] 파산 후 영구 구매 단계로 이동
    /// </summary>
    public bool LoadLegacyShopScene()
    {
        return TryTransitionToScene(_legacyShopSceneName);
    }
    #endregion

    #region 커스텀 (private)
    private bool TryTransitionToScene(string sceneName)
    {
        if (_isTransitioning)
        {
            return false;
        }

        if (_fadeCanvasGroup == null)
        {
            Debug.LogError("페이드 CanvasGroup이 연결되지 않았습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"씬을 찾을 수 없습니다: {sceneName}");
            return false;
        }

        _isTransitioning = true;
        StartCoroutine(TransitionScene(sceneName));

        return true;
    }

    private IEnumerator TransitionScene(string sceneName)
    {
        // 1. 페이드 아웃
        _fadeCanvasGroup.blocksRaycasts = true;
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. 씬 비동기 로드
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError($"씬 로딩 요청 실패: {sceneName}");
            _fadeCanvasGroup.blocksRaycasts = false;
            _isTransitioning = false;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        // 새 씬의 UI가 표시될 수 있도록 한 프레임 대기
        yield return null;

        if (sceneName == _gameSceneName)
        {
            GameSceneBootstrap gameSceneBootstrap = FindObjectOfType<GameSceneBootstrap>();
            while (gameSceneBootstrap != null && !gameSceneBootstrap.IsInitializationComplete)
            {
                yield return null;
            }
        }

        // 3. 페이드 인
        yield return StartCoroutine(Fade(1f, 0f));

        _fadeCanvasGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    private IEnumerator Fade(float startAlpah, float targetAlpha)
    {

        if (_fadeDuration <= 0f)
        {
            _fadeCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / _fadeDuration);

            _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpah, targetAlpha, progress);

            yield return null;
        }

        _fadeCanvasGroup.alpha = targetAlpha;
    }
    #endregion

}
