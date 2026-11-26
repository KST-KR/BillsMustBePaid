using UnityEngine;

/// <summary>
/// 게임에서 사용하는 정적 데이터 에셋을 모아서 관리
/// Addressables를 통해 하나의 카탈로그로 로딩
/// </summary>
[CreateAssetMenu(fileName = "GameDataCatalog", menuName = "Game/Data Catalog")]
public class GameDataCatalog : ScriptableObject
{
	#region 성장 데이터
	[Header("성장 데이터")]
	[SerializeField] private UpgradeData[] _upgrades;
	[SerializeField] private GrowthSkillSetData[] _skillSets;

	public UpgradeData[] Upgrades => _upgrades;
	public GrowthSkillSetData[] SkillSets => _skillSets;
	#endregion

	#region 장비 데이터
	[Header("장비 데이터")]
	[SerializeField] private EquipmentDatabase _equipmentDatabase;

	public EquipmentDatabase EquipmentDatabase => _equipmentDatabase;
	#endregion

	#region 저금통 데이터
	[Header("저금통 데이터")]
	[SerializeField] private PigBankDataBaseSO _pigBankDatabase;

	public PigBankDataBaseSO PigBankDatabase => _pigBankDatabase;
	#endregion

	#region 고지서 데이터
	[Header("고지서 데이터")]
	[SerializeField] private BillData[] _billRules;

	public BillData[] BillRules => _billRules;
	#endregion

	#region 영구 상점 데이터
	[SerializeField] private LegacyShopItemData[] _legacyShopItem;

	public LegacyShopItemData[] LegacyShopItems => _legacyShopItem;
    #endregion

    #region 퍼크 데이터
    [Header("퍼크 데이터")]
    [SerializeField] private PerkData[] _perks;

    public PerkData[] Perks => _perks;
	#endregion

	#region 해금 데이터
	[Header("해금 데이터")]
    [SerializeField] private PigBankUnlockData _pigBankUnlockData;

    public PigBankUnlockData PigBankUnlockData => _pigBankUnlockData;
    #endregion

    #region 파괴 레벨 데이터
    [Header("파괴 레벨 데이터")]
    [SerializeField] private PigCrashLevelData _pigCrashLevelData;

    public PigCrashLevelData PigCrashLevelData => _pigCrashLevelData;
    #endregion

    #region 데이터 검증
    /// <summary>
    /// 카탈로그에 필요한 정적 데이터가 정상적으로 연결되어 있는지 확인
    /// </summary>
    public bool Validate()
    {
        // 성장 데이터
        if (_upgrades == null)
        {
            Debug.LogError("UpgradeData가 설정되지 않았습니다.");
            return false;
        }

        if (_skillSets == null)
        {
            Debug.LogError("GrowthSkillSetData가 설정되지 않았습니다.");
            return false;
        }

        // 장비 데이터
        if (_equipmentDatabase == null)
        {
            Debug.LogError("EquipmentDatabase가 설정되지 않았습니다.");
            return false;
        }

        // 저금통 데이터
        if (_pigBankDatabase == null)
        {
            Debug.LogError("PigBankDatabase가 설정되지 않았습니다.");
            return false;
        }

        // 고지서 데이터
        if (_billRules == null)
        {
            Debug.LogError("BillRules가 설정되지 않았습니다.");
            return false;
        }

        // 영구 상점 데이터
        if (_legacyShopItem == null)
        {
            Debug.LogError("LegacyShopItems가 설정되지 않았습니다.");
            return false;
        }

        // 퍼크 데이터
        if (_perks == null)
        {
            Debug.LogError("Perks가 설정되지 않았습니다.");
            return false;
        }

        // 저금통 해금 데이터
        if (_pigBankUnlockData == null)
        {
            Debug.LogError("PigBankUnlockData가 설정되지 않았습니다.");
            return false;
        }

        // 저금통 파괴 레벨 데이터
        if (_pigCrashLevelData == null)
        {
            Debug.LogError("PigCrashLevelData가 설정되지 않았습니다.");
            return false;
        }

        return true;
    }
    #endregion
}
