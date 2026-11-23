using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// Addressables 시스템으로 일괄 이전하는 에디터 마이그레이션
/// </summary>
public static class PigBankAddressablesMigration
{
    private const string PigBankDataPath = "Assets/Project/SO/PigBank";
    private const string PigBankPrefabGroupName = "Pig Bank Prefabs";

    [MenuItem("Tools/Addressables/Separate Pig Bank Prefabs")]
    public static void SeparatePigBankPrefabs()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || settings.DefaultGroup == null)
        {
            Debug.LogError("Addressables 설정 또는 기본 로컬 그룹(Default Local Group)이 누락");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup(PigBankPrefabGroupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                PigBankPrefabGroupName,
                false,
                false,
                false,
                settings.DefaultGroup.Schemas);
        }

        BundledAssetGroupSchema bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
        if (bundledSchema == null)
        {
            Debug.LogError($"{group.Name}에 번들형 에셋 그룹 스키마가 없음.");
            return;
        }

        bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        EditorUtility.SetDirty(bundledSchema);

        string[] assetGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { PigBankDataPath });
        int movedCount = 0;
        foreach (string assetGuid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (!(asset is PigBankGameDataSO pigData))
            {
                continue;
            }

            string prefabGuid = pigData.PigPrefabReference != null ? pigData.PigPrefabReference.AssetGUID : null;
            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogWarning($"{assetPath}에 Addressable 프리팹 참조가 없어서 건너뜀.");
                continue;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(prefabGuid, group);
            if (entry == null)
            {
                Debug.LogError($"프리팹을 {group.Name}(으)로 이동할 수 없음: {assetPath}");
                continue;
            }

            movedCount++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, bundledSchema, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"개별 패킹 기능을 사용하여 {movedCount}개의 저금통 프리팹 참조를 {group.Name}으로 이동");
    }

    [MenuItem("Tools/Addressables/Migrate Pig Bank Prefabs")]
    public static void MigratePigBankPrefabs()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || settings.DefaultGroup == null)
        {
            Debug.LogError("Addressables 설정 또는 기본 로컬 그룹(Default Local Group)이 누락");
            return;
        }

        string[] assetGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { PigBankDataPath });
        int migratedCount = 0;

        foreach (string assetGuid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (!(asset is PigBankGameDataSO pigData))
            {
                continue;
            }

            SerializedObject serializedData = new SerializedObject(pigData);
            SerializedProperty referenceProperty = serializedData.FindProperty("_pigPrefabReference");
            SerializedProperty legacyPrefabProperty = serializedData.FindProperty("_pigPrefab");
            if (referenceProperty == null || legacyPrefabProperty == null)
            {
                Debug.LogError($"프리팹 필드들이 누락: {assetPath}");
                continue;
            }

            SerializedProperty referenceGuidProperty = referenceProperty.FindPropertyRelative("m_AssetGUID");
            string prefabGuid = referenceGuidProperty != null ? referenceGuidProperty.stringValue : null;

            if (string.IsNullOrEmpty(prefabGuid))
            {
                Object prefab = legacyPrefabProperty.objectReferenceValue;
                if (prefab == null)
                {
                    Debug.LogWarning($"할당된 프리팹이 없어서 건너뜀: {assetPath}");
                    continue;
                }

                prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            }

            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogError($"프리팹 GUID를 확인할 수 없음: {assetPath}");
                continue;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(prefabGuid, settings.DefaultGroup);
            if (entry == null)
            {
                Debug.LogError($"Addressables에 프리팹을 추가할 수 없음: {assetPath}");
                continue;
            }

            entry.address = $"pigbank/{pigData.ID}";
            if (referenceGuidProperty == null)
            {
                Debug.LogError($"AssetReference를 직렬화할 수 없음: {assetPath}");
                continue;
            }

            referenceGuidProperty.stringValue = prefabGuid;
            legacyPrefabProperty.objectReferenceValue = null;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pigData);
            migratedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"돼지 저금통 프리팹 참조 {migratedCount}개를 Addressables로 이전");
    }
}
