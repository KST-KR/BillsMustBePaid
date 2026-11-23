
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables를 통해 게임 정적 데이터를 로딩하고 관리
/// </summary>
public class GameDataLoader : PersistentSingleton<GameDataLoader>
{
    #region 인스펙터
    [Header("Addressables 설정")]
    [SerializeField] private string _catalogAddress = "GameDataCatalog";
    #endregion

    #region 내부 변수
    private AsyncOperationHandle<GameDataCatalog> _catalogHandle;
    #endregion

    #region 프로퍼티
    public GameDataCatalog Catalog { get; private set; }
    public bool IsLoaded => Catalog != null;

    // 전체 데이터 로딩 진행률 (0 ~ 1)
    public float LoadProgress { get; private set; }
    #endregion

    #region 싱글톤 정리
    protected override void OnSingletonDestroy()
    {
        // 카탈로그 사용이 끝나면 Addressables 핸들 해제
        if (_catalogHandle.IsValid())
        {
            Addressables.Release(_catalogHandle);
        }

        Catalog = null;
    }
    #endregion

    #region 데이터 로딩
    /// <summary>
    /// Addressables 초기화 및 게임 데이터 카탈로그 로딩
    /// </summary>
    public IEnumerator LoadCatalog()
    {
        // 이미 로딩된 경우 중복 로딩 방지
        if (IsLoaded)
        {
            LoadProgress = 1f;
            yield break;
        }

        LoadProgress = 0f;

        if (string.IsNullOrWhiteSpace(_catalogAddress))
        {
            Debug.LogError("카탈로그 Address가 설정되지 않았습니다.");
            yield break;
        }

        // Addressables 초기화
        AsyncOperationHandle initHandle = Addressables.InitializeAsync(false);

        // 초기화 진행률 반영 (전체 20%)
        while (!initHandle.IsDone)
        {
            LoadProgress = initHandle.PercentComplete * 0.18f;
            yield return null;
        }

        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Addressables 초기화 실패");

            Addressables.Release(initHandle);
            yield break;
        }

        Addressables.Release(initHandle);

        // 카탈로그 로딩
        _catalogHandle = Addressables.LoadAssetAsync<GameDataCatalog>(_catalogAddress);

        // 카탈로그 로딩 진행률 반영 (전체 80%)
        while (!_catalogHandle.IsDone)
        {
            LoadProgress = 0.18f + (_catalogHandle.PercentComplete * 0.72f);
            yield return null;
        }

        if (_catalogHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"카탈로그 로딩 실패 - {_catalogAddress}");

            if (_catalogHandle.IsValid())
            {
                Addressables.Release(_catalogHandle);
            }

            _catalogHandle = default;
            yield break;
        }

        Catalog = _catalogHandle.Result;

        if (Catalog == null)
        {
            Debug.LogError("로딩된 카탈로그가 null입니다.");

            Addressables.Release(_catalogHandle);
            _catalogHandle = default;
            yield break;
        }

        LoadProgress = 0.9f;

        Debug.Log("게임 데이터 카탈로그 로딩 완료");
    }

    public void ReportInitialPrefabPreparation(float progress)
    {
        LoadProgress = Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(progress));
    }
    #endregion
}
