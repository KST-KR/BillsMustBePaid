using UnityEngine;

public class GameDataLoaderTester : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(TestLoadCatalog());
    }

    private System.Collections.IEnumerator TestLoadCatalog()
    {
        // 카탈로그 로딩
        yield return GameDataLoader.Instance.LoadCatalog();

        // 로딩 결과 확인
        if (!GameDataLoader.Instance.IsLoaded)
        {
            Debug.LogError("카탈로그 로딩 테스트 실패");
            yield break;
        }

        GameDataCatalog catalog = GameDataLoader.Instance.Catalog;

        Debug.Log("카탈로그 로딩 테스트 성공");

        // 내부 데이터 확인
        Debug.Log($"성장 데이터 개수: {catalog.Upgrades.Length}");
        Debug.Log($"성장 스킬 세트 개수: {catalog.SkillSets.Length}");

        Debug.Log($"장비 데이터베이스: {catalog.EquipmentDatabase}");
        Debug.Log($"저금통 데이터베이스: {catalog.PigBankDatabase}");

        Debug.Log($"고지서 데이터 개수: {catalog.BillRules.Length}");
        Debug.Log($"영구 상점 데이터 개수: {catalog.LegacyShopItems.Length}");

        Debug.Log($"퍼크 데이터 개수: {catalog.Perks.Length}");
        Debug.Log($"저금통 해금 데이터: {catalog.PigBankUnlockData}");
        Debug.Log($"저금통 파괴 레벨 데이터: {catalog.PigCrashLevelData}");
    }
}