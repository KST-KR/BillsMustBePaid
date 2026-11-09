using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PL-ASKILL-01] 엑티브 스킬 슬롯과 사용 상태 관리
/// 쿨타임 진행과 사용 이벤트를 전달
/// </summary>
public sealed class ActiveSkillManager : MonoBehaviour
{
    #region 상수
    private const int CMaxSlots = 5;
    private const int CMaxBuffSlots = 3;
    #endregion

    #region 인스펙터
    [SerializeField] private GrowthManager _growthManager;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private KeyCode[] _slotKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5
    };
    #endregion

    #region 이벤트
    public event Action OnSlotDataChanged;
    public event Action<int, UpgradeData> OnActiveSkillUsed;
    public event Action<string, float> OnActiveBuffStarted;
    public event Action<string, float> OnActiveBuffRefreshed;
    public event Action<string> OnActiveBuffEnded;
    #endregion

    #region 내부 변수
    private readonly SlotState[] _slots =
    {
        new SlotState(),
        new SlotState(),
        new SlotState(),
        new SlotState(),
        new SlotState()
    };
    private readonly List<ActiveBuffState> _activeBuffs = new List<ActiveBuffState>(CMaxBuffSlots);
    #endregion

    #region 속성
    public int SlotCount => CMaxSlots;
    #endregion

    #region 이벤트 함수
    private void Start()
    {
        ResolveDependencies();

        if (_growthManager != null)
        {
            _growthManager.OnGrowthChanged += HandleGrowthChanged;
        }

        RebuildSlots();
    }

    private void OnDestroy()
    {
        if (_growthManager != null)
        {
            _growthManager.OnGrowthChanged -= HandleGrowthChanged;
        }
    }

    private void Update()
    {
        TickCooldowns();
        HandleInput();
    }
    #endregion

    #region 커스텀 (Public)
    public bool HasAnyEquippedSkill()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].Skill != null)
            {
                return true;
            }
        }

        return false;
    }

    public UpgradeData GetSlotSkill(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return null;
        }

        return _slots[slotIndex].Skill;
    }

    public float GetSlotCooldownRemaining(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return 0.0f;
        }

        return _slots[slotIndex].CooldownRemaining;
    }

    public float GetSlotCooldownDuration(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return 0.0f;
        }

        return _slots[slotIndex].CooldownDuration;
    }

    public float GetSlotCooldownNormalized(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return 0.0f;
        }

        SlotState slot = _slots[slotIndex];
        if (slot.CooldownDuration <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp01(slot.CooldownRemaining / slot.CooldownDuration);
    }

    public bool TryUseSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        SlotState slot = _slots[slotIndex];
        if (slot.Skill == null || slot.CooldownRemaining > 0.0f || !CanUseSkillNow())
        {
            return false;
        }

        float cooldown = slot.CooldownDuration;
        slot.CooldownRemaining = cooldown;
        RegisterDurationBuff(slot.Skill);
        OnActiveSkillUsed?.Invoke(slotIndex, slot.Skill);
        OnSlotDataChanged?.Invoke();
        return true;
    }

    public int GetActiveBuffCount()
    {
        return _activeBuffs.Count;
    }

    public UpgradeData GetActiveBuffSkill(int buffIndex)
    {
        if (buffIndex < 0 || buffIndex >= _activeBuffs.Count)
        {
            return null;
        }

        return _activeBuffs[buffIndex].Skill;
    }

    public float GetActiveBuffNormalized(int buffIndex)
    {
        if (buffIndex < 0 || buffIndex >= _activeBuffs.Count)
        {
            return 0.0f;
        }

        ActiveBuffState buff = _activeBuffs[buffIndex];
        if (buff.Duration <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp01(buff.Remaining / buff.Duration);
    }

    public bool IsBuffActive(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId))
        {
            return false;
        }

        return GetBuffIndex(upgradeId) >= 0;
    }

    public bool TryGetBuffTime(string upgradeId, out float remaining, out float duration)
    {
        remaining = 0.0f;
        duration = 0.0f;

        if (string.IsNullOrEmpty(upgradeId))
        {
            return false;
        }

        int index = GetBuffIndex(upgradeId);
        if (index < 0)
        {
            return false;
        }

        ActiveBuffState buff = _activeBuffs[index];
        remaining = buff.Remaining;
        duration = buff.Duration;
        return true;
    }
    #endregion

    #region 커스텀 (Private)
    private void ResolveDependencies()
    {
        if (_growthManager == null)
        {
            _growthManager = GrowthManager.Instance;
        }

        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;
        }

        if (_stageManager == null)
        {
            _stageManager = StageManager.Instance;
        }
    }

    private void HandleGrowthChanged()
    {
        RebuildSlots();
    }

    private void RebuildSlots()
    {
        if (_growthManager == null)
        {
            ClearSlots();
            OnSlotDataChanged?.Invoke();
            return;
        }

        IReadOnlyList<UpgradeData> learnedSkills = _growthManager.GetLearnedActiveSkills();
        Dictionary<string, float> previousCooldowns = new Dictionary<string, float>();

        for (int i = 0; i < _slots.Length; i++)
        {
            UpgradeData previousSkill = _slots[i].Skill;
            if (previousSkill == null)
            {
                continue;
            }

            previousCooldowns[previousSkill.UpgradeId] = _slots[i].CooldownRemaining;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            SlotState slot = _slots[i];
            UpgradeData skill = i < learnedSkills.Count ? learnedSkills[i] : null;
            slot.Skill = skill;

            if (skill == null)
            {
                slot.CooldownDuration = 0.0f;
                slot.CooldownRemaining = 0.0f;
                continue;
            }

            float cooldown = _growthManager.GetActiveSkillCooldownSec(skill.UpgradeId);
            slot.CooldownDuration = cooldown;

            if (previousCooldowns.TryGetValue(skill.UpgradeId, out float previousRemaining))
            {
                slot.CooldownRemaining = Mathf.Clamp(previousRemaining, 0.0f, cooldown);
            }
            else
            {
                slot.CooldownRemaining = 0.0f;
            }
        }

        SyncActiveBuffsWithLearnedSkills();

        OnSlotDataChanged?.Invoke();
    }

    private void HandleInput()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            KeyCode keyCode = i < _slotKeys.Length ? _slotKeys[i] : KeyCode.None;
            if (keyCode == KeyCode.None)
            {
                continue;
            }

            if (Input.GetKeyDown(keyCode))
            {
                TryUseSlot(i);
            }
        }
    }

    private void TickCooldowns()
    {
        bool changed = false;

        for (int i = 0; i < _slots.Length; i++)
        {
            SlotState slot = _slots[i];
            if (slot.CooldownRemaining <= 0.0f)
            {
                continue;
            }

            slot.CooldownRemaining = Mathf.Max(0.0f, slot.CooldownRemaining - Time.deltaTime);
            changed = true;
        }

        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuffState buff = _activeBuffs[i];
            if (buff.Remaining <= 0.0f)
            {
                NotifyBuffEnded(buff.Skill);
                _activeBuffs.RemoveAt(i);
                changed = true;
                continue;
            }

            buff.Remaining = Mathf.Max(0.0f, buff.Remaining - Time.deltaTime);
            _activeBuffs[i] = buff;
            changed = true;

            if (buff.Remaining <= 0.0f)
            {
                NotifyBuffEnded(buff.Skill);
                _activeBuffs.RemoveAt(i);
            }
        }

        if (changed)
        {
            OnSlotDataChanged?.Invoke();
        }
    }

    private bool CanUseSkillNow()
    {
        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;
        }

        if (_stageManager == null)
        {
            _stageManager = StageManager.Instance;
        }

        if (_gameManager == null || _stageManager == null)
        {
            return false;
        }

        return _gameManager.State == EGameState.Playing && !_gameManager.IsPaused && _stageManager.IsStageActive;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _slots.Length;
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Skill = null;
            _slots[i].CooldownDuration = 0.0f;
            _slots[i].CooldownRemaining = 0.0f;
        }

        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            NotifyBuffEnded(_activeBuffs[i].Skill);
        }

        _activeBuffs.Clear();
    }

    private void RegisterDurationBuff(UpgradeData skill)
    {
        if (_growthManager == null || skill == null)
        {
            return;
        }

        float duration = _growthManager.GetUpgradeDurationSec(skill.UpgradeId);
        if (duration <= 0.0f)
        {
            return;
        }

        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].Skill != null && _activeBuffs[i].Skill.UpgradeId == skill.UpgradeId)
            {
                ActiveBuffState refreshed = _activeBuffs[i];
                refreshed.Duration = duration;
                refreshed.Remaining = duration;
                _activeBuffs[i] = refreshed;
                OnActiveBuffRefreshed?.Invoke(skill.UpgradeId, duration);
                return;
            }
        }

        ActiveBuffState newBuff = new ActiveBuffState
        {
            Skill = skill,
            Duration = duration,
            Remaining = duration
        };

        if (_activeBuffs.Count < CMaxBuffSlots)
        {
            _activeBuffs.Add(newBuff);
            OnActiveBuffStarted?.Invoke(skill.UpgradeId, duration);
            return;
        }

        int replaceIndex = 0;
        float minRemaining = _activeBuffs[0].Remaining;
        for (int i = 1; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].Remaining < minRemaining)
            {
                minRemaining = _activeBuffs[i].Remaining;
                replaceIndex = i;
            }
        }

        NotifyBuffEnded(_activeBuffs[replaceIndex].Skill);
        _activeBuffs[replaceIndex] = newBuff;
        OnActiveBuffStarted?.Invoke(skill.UpgradeId, duration);
    }

    private void SyncActiveBuffsWithLearnedSkills()
    {
        if (_growthManager == null)
        {
            _activeBuffs.Clear();
            return;
        }

        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuffState buff = _activeBuffs[i];
            if (buff.Skill == null || _growthManager.GetLevel(buff.Skill.UpgradeId) <= 0)
            {
                NotifyBuffEnded(buff.Skill);
                _activeBuffs.RemoveAt(i);
                continue;
            }

            float duration = _growthManager.GetUpgradeDurationSec(buff.Skill.UpgradeId);
            if (duration <= 0.0f)
            {
                NotifyBuffEnded(buff.Skill);
                _activeBuffs.RemoveAt(i);
                continue;
            }

            buff.Duration = duration;
            buff.Remaining = Mathf.Clamp(buff.Remaining, 0.0f, duration);
            _activeBuffs[i] = buff;
        }
    }

    private int GetBuffIndex(string upgradeId)
    {
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            UpgradeData skill = _activeBuffs[i].Skill;
            if (skill != null && skill.UpgradeId == upgradeId)
            {
                return i;
            }
        }

        return -1;
    }

    private void NotifyBuffEnded(UpgradeData skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.UpgradeId))
        {
            return;
        }

        OnActiveBuffEnded?.Invoke(skill.UpgradeId);
    }
    #endregion

    private sealed class SlotState
    {
        public UpgradeData Skill;
        public float CooldownDuration;
        public float CooldownRemaining;
    }

    private struct ActiveBuffState
    {
        public UpgradeData Skill;
        public float Duration;
        public float Remaining;
    }
}
