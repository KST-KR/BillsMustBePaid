using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 고지서 납부 기반 퍼크 선택 흐름 관리
/// </summary>
public sealed class PerkManager : Singleton<PerkManager>
{
    #region 상수
    private const string CTonyLoanBillTypeId = "TONY_LOAN"; 
    #endregion

    #region 이벤트
    public event Action<IReadOnlyList<PerkData>, BillRecord> OnPerkOfferStarted;
    public event Action<PerkData> OnPerkSelected;
    public event Action OnPerkStateChanged;
    #endregion

    #region 인스펙터
    [SerializeField] private PerkData[] _perks = new PerkData[0];
    #endregion

    #region 내부 변수
    private readonly List<PerkData> _ownedPerks = new List<PerkData>();
    private readonly List<PerkData> _offeredPerks = new List<PerkData>();
    private readonly Dictionary<EPerkTargetStat, int> _nextTierByStat = new Dictionary<EPerkTargetStat, int>();
    private BillRecord _currentOfferBill;
    private BillManager _billManager;
    private SaveManager _saveManager;
    private GameManager _gameManager;
    #endregion

    #region 속성
    public IReadOnlyList<PerkData> OwnedPerks => _ownedPerks;
    public IReadOnlyList<PerkData> OfferedPerks => _offeredPerks;
    public bool IsOffering => _offeredPerks.Count > 0;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        InitNextTierDictionary();
    }

    private void Start()
    {
        _billManager = BillManager.Instance;
        _saveManager = SaveManager.Instance;
        _gameManager = GameManager.Instance;

        if (_billManager != null)
        {
            _billManager.OnBillPaid += HandleBillPaid;
        }

        if (_gameManager != null)
        {
            _gameManager.OnRunStarted += HandleRunStarted;
        }

        if (_saveManager != null)
        {
            _saveManager.OnGameLoaded += HandleGameLoaded;
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (_billManager != null)
        {
            _billManager.OnBillPaid -= HandleBillPaid;
        }

        if (_gameManager != null)
        {
            _gameManager.OnRunStarted -= HandleRunStarted;
        }

        if (_saveManager != null)
        {
            _saveManager.OnGameLoaded -= HandleGameLoaded;
        }
    }
    #endregion

    #region 커스텀 Public
    public void ConfigurePerks(PerkData[] perks)
    {
        _perks = perks != null ? perks : new PerkData[0];
    }

    public bool TryStartPerkOffer(BillRecord bill)
    {
        if (bill == null || bill.PerkRewardCount <= 0 || string.Equals(bill.BillTypeId, CTonyLoanBillTypeId, StringComparison.OrdinalIgnoreCase) || _perks == null || _perks.Length == 0)
        {
            return false;
        }

        _offeredPerks.Clear();
        _currentOfferBill = bill;

        List<PerkData> candidates = BuildOfferCandidates();
        if (candidates.Count == 0)
        {
            _currentOfferBill = null;
            return false;
        }

        int rewardCount = Mathf.Max(1, bill.PerkRewardCount);
        PickRandomByLowTierPriority(candidates, rewardCount, _offeredPerks);
        if (_offeredPerks.Count == 0)
        {
            _currentOfferBill = null;
            return false;
        }

        OnPerkOfferStarted?.Invoke(_offeredPerks, bill);
        OnPerkStateChanged?.Invoke();
        return true;
    }

    public bool TrySelectPerk(string perkId)
    {
        if (string.IsNullOrWhiteSpace(perkId) || _offeredPerks.Count == 0)
        {
            return false;
        }

        PerkData selected = null;
        for (int i = 0; i < _offeredPerks.Count; i++)
        {
            if (_offeredPerks[i] != null && string.Equals(_offeredPerks[i].PerkId, perkId, StringComparison.Ordinal))
            {
                selected = _offeredPerks[i];
                break;
            }
        }

        if (selected == null)
        {
            return false;
        }

        ApplySelectedPerk(selected);
        return true;
    }

    public bool HasOwnedPerk(EPerkTargetStat stat)
    {
        for (int i = 0; i < _ownedPerks.Count; i++)
        {
            if (_ownedPerks[i] != null && _ownedPerks[i].TargetStat == stat)
            {
                return true;
            }
        }

        return false;
    }

    public int GetNextTier(EPerkTargetStat stat)
    {
        return _nextTierByStat.TryGetValue(stat, out int nextTier) ? nextTier : 1;
    }

    public float GetTotalFlat(EPerkTargetStat stat)
    {
        float total = 0.0f;
        for (int i = 0; i < _ownedPerks.Count; i++)
        {
            PerkData perk = _ownedPerks[i];
            if (perk == null || perk.TargetStat != stat || perk.ValueMode != EPerkValueMode.Flat)
            {
                continue;
            }

            total += perk.Value;
        }

        return total;
    }

    public float GetTotalPercent(EPerkTargetStat stat)
    {
        float total = 0.0f;
        for (int i = 0; i < _ownedPerks.Count; i++)
        {
            PerkData perk = _ownedPerks[i];
            if (perk == null || perk.TargetStat != stat || perk.ValueMode != EPerkValueMode.Percent)
            {
                continue;
            }

            total += perk.Value;
        }

        return total;
    }

    public RunPerkSaveData ExportSaveData()
    {
        RunPerkSaveData saveData = new RunPerkSaveData();
        for (int i = 0; i < _ownedPerks.Count; i++)
        {
            if (_ownedPerks[i] != null && !string.IsNullOrEmpty(_ownedPerks[i].PerkId))
            {
                saveData.ownedPerkIds.Add(_ownedPerks[i].PerkId);
            }
        }

        saveData.nextTierByStat.Clear();
        for (int i = 0; i < CStatOrder.Length; i++)
        {
            saveData.nextTierByStat.Add(GetNextTier(CStatOrder[i]));
        }

        return saveData;
    }

    public void ImportSaveData(RunPerkSaveData saveData)
    {
        ResetRunPerks();
        if (saveData == null)
        {
            return;
        }

        if (saveData.nextTierByStat != null)
        {
            int count = Mathf.Min(saveData.nextTierByStat.Count, CStatOrder.Length);
            for (int i = 0; i < count; i++)
            {
                _nextTierByStat[CStatOrder[i]] = Mathf.Clamp(saveData.nextTierByStat[i], 1, 6);
            }
        }

        if (saveData.ownedPerkIds != null)
        {
            for (int i = 0; i < saveData.ownedPerkIds.Count; i++)
            {
                PerkData perk = FindPerkById(saveData.ownedPerkIds[i]);
                if (perk != null)
                {
                    _ownedPerks.Add(perk);
                }
            }
        }

        OnPerkStateChanged?.Invoke();
    }

    public void ResetRunPerks()
    {
        _ownedPerks.Clear();
        _offeredPerks.Clear();
        _currentOfferBill = null;
        InitNextTierDictionary();
        OnPerkStateChanged?.Invoke();
    }
    #endregion

    #region 커스텀 Private
    private static readonly EPerkTargetStat[] CStatOrder =
    {
        EPerkTargetStat.Attack,
        EPerkTargetStat.AttackSpeed,
        EPerkTargetStat.Stamina,
        EPerkTargetStat.AttackRange,
        EPerkTargetStat.Reward,
        EPerkTargetStat.CriticalChance
    };

    private void HandleBillPaid(BillRecord bill)
    {
        TryStartPerkOffer(bill);
    }

    private void HandleRunStarted()
    {
        if (_saveManager == null || _saveManager.CurrentSlotIndex < 1 || !_saveManager.HasSaveData(_saveManager.CurrentSlotIndex))
        {
            ResetRunPerks();
        }
    }

    private void HandleGameLoaded(SaveSlotData saveData)
    {
        if (saveData == null || saveData.runData == null)
        {
            return;
        }

        ImportSaveData(saveData.runData.runPerks);
    }

    private void InitNextTierDictionary()
    {
        _nextTierByStat.Clear();
        for (int i = 0; i < CStatOrder.Length; i++)
        {
            _nextTierByStat[CStatOrder[i]] = 1;
        }
    }

    private List<PerkData> BuildOfferCandidates()
    {
        Dictionary<EPerkTargetStat, PerkData> bestByStat = new Dictionary<EPerkTargetStat, PerkData>();
        for (int i = 0; i < _perks.Length; i++)
        {
            PerkData perk = _perks[i];
            if (perk == null)
            {
                continue;
            }

            int targetTier = GetNextTier(perk.TargetStat);
            if (targetTier > 5 || perk.Tier != targetTier)
            {
                continue;
            }

            if (!bestByStat.ContainsKey(perk.TargetStat))
            {
                bestByStat.Add(perk.TargetStat, perk);
            }
        }

        return new List<PerkData>(bestByStat.Values);
    }

    private void PickRandomByLowTierPriority(List<PerkData> source, int pickCount, List<PerkData> output)
    {
        output.Clear();
        List<PerkData> pool = new List<PerkData>(source);

        while (pool.Count > 0 && output.Count < pickCount)
        {
            int minTier = int.MaxValue;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].Tier < minTier)
                {
                    minTier = pool[i].Tier;
                }
            }

            List<int> sameTierIndices = new List<int>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].Tier == minTier)
                {
                    sameTierIndices.Add(i);
                }
            }

            while (sameTierIndices.Count > 0 && output.Count < pickCount)
            {
                int randomListIndex = UnityEngine.Random.Range(0, sameTierIndices.Count);
                int pickedPoolIndex = sameTierIndices[randomListIndex];
                output.Add(pool[pickedPoolIndex]);
                pool.RemoveAt(pickedPoolIndex);

                sameTierIndices.Clear();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i] != null && pool[i].Tier == minTier)
                    {
                        sameTierIndices.Add(i);
                    }
                }
            }
        }
    }

    private void ApplySelectedPerk(PerkData perk)
    {
        if (perk == null)
        {
            return;
        }

        _ownedPerks.Add(perk);

        int nextTier = Mathf.Clamp(perk.Tier + 1, 1, 6);
        _nextTierByStat[perk.TargetStat] = Mathf.Max(GetNextTier(perk.TargetStat), nextTier);

        _offeredPerks.Clear();
        _currentOfferBill = null;

        OnPerkSelected?.Invoke(perk);
        OnPerkStateChanged?.Invoke();
    }

    private PerkData FindPerkById(string perkId)
    {
        if (string.IsNullOrWhiteSpace(perkId) || _perks == null)
        {
            return null;
        }

        for (int i = 0; i < _perks.Length; i++)
        {
            if (_perks[i] != null && string.Equals(_perks[i].PerkId, perkId, StringComparison.Ordinal))
            {
                return _perks[i];
            }
        }

        return null;
    }
    #endregion
}
