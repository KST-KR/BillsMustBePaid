using System.Collections;
using UnityEngine;

public class GameDataLoaderTester : MonoBehaviour
{
    #region 인스펙터
    [Header("로딩 화면")]
    [SerializeField] private LoadingScreenController _loadingScreen;
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
        // 카탈로그 로딩
        yield return GameDataLoader.Instance.LoadCatalog();

        // 로딩 결과 확인
        if (!GameDataLoader.Instance.IsLoaded)
        {
            Debug.LogError("카탈로그 로딩 실패");
            yield break;
        }

        // 데이터 로딩이 끝난 후 메인 메뉴 표시
        if (_loadingScreen != null)
        {
            yield return _loadingScreen.FadeIn();
        }
        else
        {
            Debug.LogError("LoadingScreenController가 연결되지 않았습니다.");
        }
    }
    #endregion
}