using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [GROW-01-01][GROW-01-05] 스킬 트리 진행 데이터 및 저장 연동 계약 관리
/// </summary>
public class GrowthManager : MonoBehaviour
{
    #region 싱글톤
    public static GrowthManager Instance { get; private set; }
    #endregion

    #region 속성
    public IReadOnlyList<UpgradeData> Upgrades => _upgrades;
    public string SelectedUpgradeId => _selectedUpgradeId;
    #endregion

    #region 이벤트
    public event Action<string, int, float> OnUpgradePurchased;
    /// <summary>
    /// [GROW-01-04] 스텟쪽에서 호출필요 스킬 성장값을 적용, amount는 변경된 값
    /// </summary>
    public event Action<UpgradeData, float> OnApplyStatGrowth;
    public event Action OnGrowthChanged;
    public event Action<string> OnSelectionChanged;
    public event Action OnResetRunGrowth;
    #endregion

    #region 내부 변수
    private UpgradeData[] _upgrades = new UpgradeData[0];
    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
    private readonly HashSet<string> _unlockedIds = new HashSet<string>();
    private string _selectedUpgradeId;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _upgrades = CreateMockUpgrades();
    }

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameLoaded += HandleGameLoaded;
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameLoaded -= HandleGameLoaded;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 커스텀 (Public)
    /// <summary>
    /// [GROW-01-01] ID에 해당하는 스킬 데이터 반환
    /// </summary>
    public UpgradeData GetUpgrade(string upgradeId)
    {
        for (int i = 0; i < _upgrades.Length; i++)
        {
            if (_upgrades[i] != null && _upgrades[i].UpgradeId == upgradeId)
            {
                return _upgrades[i];
            }
        }

        return null;
    }

    /// <summary>
    /// [GROW-01-01] 해금 조건, 최대 레벨, 골드 잔고 기준 구매 가능 여부 반환
    /// </summary>
    public bool CanPurchase(string upgradeId)
    {
        UpgradeData upgrade = GetUpgrade(upgradeId);
        if (upgrade == null || GetLevel(upgradeId) >= upgrade.MaxLevel || (!IsUnlocked(upgradeId) && !ArePrerequisitesLearned(upgrade)))
        {
            return false;
        }

        return CurrencyManager.Instance != null && CurrencyManager.Instance.Gold >= upgrade.Cost;
    }

    /// <summary>
    /// [GROW-01-03] 조건과 재화를 확인해 스킬 레벨을 올리고 효과 반영
    /// </summary>
    public bool PurchaseUpgrade(string upgradeId)
    {
        // TODO 추가 로직 필요함 (스킬 변경값 전달을 위한 초안상태)

        UpgradeData upgrade = GetUpgrade(upgradeId);
        if (upgrade == null || !CanPurchase(upgradeId) || !CurrencyManager.Instance.TrySpend(ECurrencyType.Gold, upgrade.Cost))
        {
            return false;
        }

        if (upgrade.EffectMode == ESkillEffectMode.PlayerStat)
        {
            ApplyStatGrowth(upgrade, upgrade.ValuePerLevel);
        }

        return true;
    }

    /// <summary>
    /// [GROW-01-01] 스킬의 현재 레벨 반환
    /// </summary>
    public int GetLevel(string upgradeId)
    {
        return _levels.TryGetValue(upgradeId, out int level) ? level : 0;
    }

    /// <summary>
    /// [GROW-01-01] 스킬 학습 여부 반환 (레벨 1 이상)
    /// </summary>
    public bool IsLearned(string upgradeId)
    {
        return GetLevel(upgradeId) > 0;
    }

    /// <summary>
    /// [GROW-01-01] 스킬 학습 여부 반환 (스킬 해금 조건 포함)
    /// </summary>
    public bool IsLearned(PrerequisiteUpgrade item)
    {
        return GetLevel(item.upgradeId) >= item.requiredLevel;
    }

    /// <summary>
    /// [GROW-01-01] 스킬 해금 여부 반환
    /// </summary>
    public bool IsUnlocked(string upgradeId)
    {
        return GetLevel(upgradeId) > 0 || _unlockedIds.Contains(upgradeId);
    }

    /// <summary>
    /// [GROW-01-01] 스킬 노드의 현재 상태 반환
    /// </summary>
    public EUpgradeNodeStatus GetNodeStatus(string upgradeId)
    {
        UpgradeData upgrade = GetUpgrade(upgradeId);
        if (upgrade == null)
        {
            return EUpgradeNodeStatus.Locked;
        }

        if (GetLevel(upgradeId) >= upgrade.MaxLevel)
        {
            return EUpgradeNodeStatus.MaxLevel;
        }

        if (GetLevel(upgradeId) > 0)
        {
            return CanPurchase(upgradeId) ? EUpgradeNodeStatus.Enhanceable : EUpgradeNodeStatus.Learned;
        }

        return IsUnlocked(upgradeId) || ArePrerequisitesLearned(upgrade) ? EUpgradeNodeStatus.Unlocked : EUpgradeNodeStatus.Locked;
    }

    /// <summary>
    /// [GROW-02-01] 상세 패널에 표시할 스킬 선택
    /// </summary>
    public void SelectUpgrade(string upgradeId)
    {
        if (GetUpgrade(upgradeId) == null || _selectedUpgradeId == upgradeId)
        {
            return;
        }

        _selectedUpgradeId = upgradeId;
        OnSelectionChanged?.Invoke(upgradeId);
    }

    /// <summary>
    /// [GROW-01-01] 지정 능력치에 적용된 스킬 성장값 합산
    /// </summary>
    public float GetStatValue(EUpgradeStat stat)
    {
        float total = 0.0f;
        for (int i = 0; i < _upgrades.Length; i++)
        {
            UpgradeData upgrade = _upgrades[i];
            if (upgrade != null && upgrade.EffectMode == ESkillEffectMode.PlayerStat && upgrade.TargetStat == stat)
            {
                total += GetLevel(upgrade.UpgradeId) * upgrade.ValuePerLevel;
            }
        }

        return total;
    }

    /// <summary>
    /// [GROW-01-05] 스킬 선택·레벨·해금 정보를 저장 DTO로 내보냄
    /// </summary>
    public RunGrowthSaveData ExportSaveData()
    {
        RunGrowthSaveData saveData = new RunGrowthSaveData();
        saveData.SelectedUpgradeId = _selectedUpgradeId;
        for (int i = 0; i < _upgrades.Length; i++)
        {
            UpgradeData upgrade = _upgrades[i];
            if (upgrade != null)
            {
                saveData.Skills.Add(new GrowthSkillSaveEntry(upgrade.UpgradeId, GetLevel(upgrade.UpgradeId), IsUnlocked(upgrade.UpgradeId)));
            }
        }

        return saveData;
    }

    /// <summary>
    /// [GROW-01-05] 저장 DTO의 스킬 진행 정보를 복원
    /// </summary>
    public void ImportSaveData(RunGrowthSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        _levels.Clear();
        _unlockedIds.Clear();
        if (saveData.Skills == null)
        {
            saveData.Skills = new List<GrowthSkillSaveEntry>();
        }

        for (int i = 0; i < saveData.Skills.Count; i++)
        {
            GrowthSkillSaveEntry entry = saveData.Skills[i];
            if (entry == null || string.IsNullOrEmpty(entry.UpgradeId))
            {
                continue;
            }

            UpgradeData upgrade = GetUpgrade(entry.UpgradeId);
            if (upgrade == null)
            {
                continue;
            }

            int level = Mathf.Clamp(entry.Level, 0, upgrade.MaxLevel);
            _levels[entry.UpgradeId] = level;
            if (entry.IsUnlocked || level > 0)
            {
                _unlockedIds.Add(entry.UpgradeId);
            }

            if (upgrade.EffectMode == ESkillEffectMode.PlayerStat && level > 0)
            {
                float value = level * upgrade.ValuePerLevel;
                ApplyStatGrowth(upgrade, value);
            }
        }

        _selectedUpgradeId = GetUpgrade(saveData.SelectedUpgradeId) != null ? saveData.SelectedUpgradeId : null;
        OnGrowthChanged?.Invoke();
        OnSelectionChanged?.Invoke(_selectedUpgradeId);
    }

    /// <summary>
    /// [GROW-01-05] 다음 사이클 진행을 위해 모든 스킬 레벨과 해금 이력 초기화
    /// </summary>
    public void ResetRunGrowth()
    {
        _levels.Clear();
        _unlockedIds.Clear();
        _selectedUpgradeId = null;
        OnGrowthChanged?.Invoke();
        OnSelectionChanged?.Invoke(null);
        OnResetRunGrowth?.Invoke();
    }

    /// <summary>
    /// [GROW-01-04] 능력치 초기화 직후 보유 스킬의 성장값 재적용
    /// </summary>
    public void ReapplyPlayerStatSkills()
    {
        for (int i = 0; i < _upgrades.Length; i++)
        {
            UpgradeData upgrade = _upgrades[i];
            int level = upgrade != null ? GetLevel(upgrade.UpgradeId) : 0;
            if (upgrade == null || level <= 0 || upgrade.EffectMode != ESkillEffectMode.PlayerStat)
            {
                continue;
            }

            float value = level * upgrade.ValuePerLevel;
            ApplyStatGrowth(upgrade, value);
        }
    }

    #endregion

    #region 커스텀 (Private)
    /// <summary>
    /// [GROW-01-01] 코드에서 사용할 목업 스킬 데이터 생성
    /// </summary>
    private UpgradeData[] CreateMockUpgrades()
    {
        return new UpgradeData[]
        {
            new UpgradeData(
                "attack_power",
                "기본 공격력",
                "공격력 증가",
                EUpgradeStat.Attack,
                50,
                2.0f,
                5,
                ESkillEffectMode.PlayerStat,
                new Vector2(-250.0f, 100.0f)),

            new UpgradeData(
                "attack_speed",
                "빠른 손",
                "공격 간격 감소",
                EUpgradeStat.AttackSpeed,
                40,
                -0.05f,
                5,
                ESkillEffectMode.PlayerStat,
                new Vector2(0f, 0f),
                new PrerequisiteUpgrade[]
                {
                    new PrerequisiteUpgrade { upgradeId = "attack_power", requiredLevel = 1 }
                }),

            new UpgradeData(
                "luck",
                "행운",
                "행운 증가",
                EUpgradeStat.Luck,
                60,
                1.0f,
                5,
                ESkillEffectMode.PlayerStat,
                new Vector2(0f, 0f)),

            new UpgradeData(
                "attack_range",
                "넓은 타격",
                "공격 범위 증가",
                EUpgradeStat.AttackRange,
                45,
                0.5f,
                3,
                ESkillEffectMode.PlayerStat,
                new Vector2(0f, 0f),
                new PrerequisiteUpgrade[]
                {
                    new PrerequisiteUpgrade { upgradeId = "luck", requiredLevel = 1 }
                })
        };
    }

    /// <summary>
    /// [GROW-01-04] 능력치형 스킬과 변경분을 플레이어 담당 이벤트 구독자에게 전달
    /// </summary>
    private void ApplyStatGrowth(UpgradeData upgrade, float amount)
    {
        if (upgrade == null || upgrade.EffectMode != ESkillEffectMode.PlayerStat || amount == 0.0f)
        {
            return;
        }

        OnApplyStatGrowth?.Invoke(upgrade, amount);
    }

    private void HandleGameLoaded(SaveSlotData saveData)
    {
        if (saveData != null)
        {
            ImportSaveData(saveData.runData.runGrowth);
        }
    }

    private bool ArePrerequisitesLearned(UpgradeData upgrade)
    {
        PrerequisiteUpgrade[] prerequisites = upgrade.PrerequisiteIds;
        for (int i = 0; i < prerequisites.Length; i++)
        {
            if (!IsLearned(prerequisites[i]))
            {
                return false;
            }
        }

        return true;
    }
    #endregion
}
