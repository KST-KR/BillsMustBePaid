using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [GROW-01-01] 스킬 트리 진행 데이터 및 저장 연동 계약 관리
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
    public event Action OnGrowthChanged;
    public event Action<string> OnSelectionChanged;
    #endregion

    #region 인스펙터
    [Header("스킬 노드")]
    [SerializeField] private UpgradeData[] _upgrades = new UpgradeData[0];
    #endregion

    #region 내부 변수
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
        if (_upgrades == null)
        {
            _upgrades = new UpgradeData[0];
        }
    }

    private void OnDestroy()
    {
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
            if (upgrade != null && upgrade.TargetStat == stat)
            {
                total += GetLevel(upgrade.UpgradeId) * upgrade.ValuePerLevel;
            }
        }

        return total;
    }

    public object ExportSaveData()
    {
        return null;
    }

    public void ImportSaveData(object saveData)
    {

    }

    /// <summary>
    /// [GROW-01-05] 현재 런의 스킬 성장 진행 정보 초기화
    /// </summary>
    public void ResetRunGrowth()
    {
        _levels.Clear();
        _unlockedIds.Clear();
        OnGrowthChanged?.Invoke();
    }
    #endregion

    #region 커스텀 (Private)
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
