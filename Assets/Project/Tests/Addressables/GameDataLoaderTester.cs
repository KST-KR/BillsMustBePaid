
using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 최초 실행 시 데이터 로딩과 로딩 화면 표시를 관리
/// </summary>
public class GameDataLoaderTester : MonoBehaviour
{
    #region 인스펙터
    [Header("로딩 화면")]
    [SerializeField] private LoadingScreenController _loadingScreen;
    [SerializeField] private LoadingScreenUI _loadingScreenUI;
    #endregion

    #region 내부 변수
    private bool _isLoadCompleted;
    #endregion

    #region 이벤트 함수
    private void Start()
    {
        StartCoroutine(LoadGameData());
    }
    #endregion

    #region 데이터 로딩
    private IEnumerator LoadGameData()
    {
        GameDataLoader dataLoader = GameDataLoader.Instance;

        if (dataLoader == null)
        {
            Debug.LogError("GameDataLoader를 찾을 수 없습니다.");
            yield break;
        }

        // 데이터 로딩 시작
        StartCoroutine(LoadCatalog(dataLoader));

        // 로딩이 완료될 때까지 진행률 UI 갱신
        while (!_isLoadCompleted)
        {
            if (_loadingScreenUI != null)
            {
                _loadingScreenUI.UpdateProgress(dataLoader.LoadProgress);
            }

            yield return null;
        }

        // 최종 진행률 반영
        if (_loadingScreenUI != null)
        {
            _loadingScreenUI.UpdateProgress(dataLoader.LoadProgress);
        }

        // 로딩 결과 확인
        if (!dataLoader.IsLoaded)
        {
            Debug.LogError("게임 데이터 카탈로그 로딩 실패");
            yield break;
        }

        // 데이터 로딩 완료 후 페이드 인
        if (_loadingScreen != null)
        {
            yield return _loadingScreen.FadeIn();
        }
    }

    /// <summary>
    /// 카탈로그 로딩 및 완료 여부 전달
    /// </summary>
    private IEnumerator LoadCatalog(GameDataLoader dataLoader)
    {
        yield return dataLoader.LoadCatalog();

        _isLoadCompleted = true;
    }
    #endregion
}
