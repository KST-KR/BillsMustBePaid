using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [PL-ASKILL-01] 엑티브 슬롯 HUD 관리
/// </summary>
public sealed class ActiveSkillHud : MonoBehaviour
{
    #region 상수
    private static readonly Color CBuffClockLowColor = new Color(0.95f, 0.20f, 0.20f, 0.68f);
    private static readonly Color CBuffClockHighColor = new Color(1.0f, 0.86f, 0.22f, 0.52f);
    private static readonly Color CBuffBackgroundLowColor = new Color(0.26f, 0.12f, 0.12f, 0.9f);
    private static readonly Color CBuffBackgroundHighColor = new Color(0.30f, 0.24f, 0.08f, 0.9f);
    #endregion

    #region 인스펙터
    [SerializeField] private ActiveSkillManager _activeSkillManager;
    [SerializeField] private SlotView[] _slotViews = new SlotView[5];
    [SerializeField] private BuffView[] _buffViews = new BuffView[3];
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (_activeSkillManager == null)
        {
            _activeSkillManager = FindAnyObjectByType<ActiveSkillManager>();
        }
    }

    private void OnEnable()
    {
        SubscribeManager();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeManager();
    }

    #endregion

    #region 커스텀 (Private)
    private void SubscribeManager()
    {
        if (_activeSkillManager == null)
        {
            _activeSkillManager = FindAnyObjectByType<ActiveSkillManager>();
        }

        if (_activeSkillManager != null)
        {
            _activeSkillManager.OnSlotDataChanged -= RefreshAll;
            _activeSkillManager.OnSlotDataChanged += RefreshAll;
        }
    }

    private void UnsubscribeManager()
    {
        if (_activeSkillManager != null)
        {
            _activeSkillManager.OnSlotDataChanged -= RefreshAll;
        }
    }

    private void RefreshAll()
    {
        if (_activeSkillManager == null)
        {
            _activeSkillManager = FindAnyObjectByType<ActiveSkillManager>();
            if (_activeSkillManager == null)
            {
                return;
            }

            _activeSkillManager.OnSlotDataChanged -= RefreshAll;
            _activeSkillManager.OnSlotDataChanged += RefreshAll;
        }

        if (_slotViews == null || _slotViews.Length == 0)
        {
            return;
        }

        for (int i = 0; i < _slotViews.Length; i++)
        {
            SlotView view = _slotViews[i];
            if (view == null || view.Root == null)
            {
                continue;
            }

            UpgradeData skill = _activeSkillManager.GetSlotSkill(i);
            bool visible = skill != null;
            view.Root.SetActive(visible);

            if (!visible)
            {
                continue;
            }

            view.Icon.sprite = skill.Icon;
            view.Icon.enabled = skill.Icon != null;

            float cooldownNormalized = _activeSkillManager.GetSlotCooldownNormalized(i);
            bool onCooldown = cooldownNormalized > 0.0f;

            view.CooldownOverlay.fillAmount = cooldownNormalized;
            view.CooldownOverlay.enabled = onCooldown;
            view.Icon.color = onCooldown
                ? new Color(0.45f, 0.45f, 0.45f, 0.9f)
                : Color.white;

            view.Background.color = onCooldown
                ? new Color(0.14f, 0.14f, 0.14f, 0.88f)
                : new Color(0.21f, 0.21f, 0.21f, 0.94f);
        }

        RefreshBuffViews();
    }

    private void RefreshBuffViews()
    {
        if (_buffViews == null || _buffViews.Length == 0)
        {
            return;
        }

        int activeBuffCount = _activeSkillManager.GetActiveBuffCount();
        for (int i = 0; i < _buffViews.Length; i++)
        {
            BuffView view = _buffViews[i];
            if (view == null || view.Root == null)
            {
                continue;
            }

            bool visible = i < activeBuffCount;
            view.Root.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            UpgradeData buffSkill = _activeSkillManager.GetActiveBuffSkill(i);
            float normalized = _activeSkillManager.GetActiveBuffNormalized(i);

            if (view.Background != null)
            {
                view.Background.color = Color.Lerp(CBuffBackgroundLowColor, CBuffBackgroundHighColor, normalized);
            }

            if (view.Icon != null)
            {
                view.Icon.sprite = buffSkill != null ? buffSkill.Icon : null;
                view.Icon.enabled = buffSkill != null && buffSkill.Icon != null;
                view.Icon.color = new Color(1.0f, 1.0f, 1.0f, 0.98f);
            }

            if (view.DurationOverlay != null)
            {
                view.DurationOverlay.fillAmount = normalized;
                view.DurationOverlay.enabled = normalized > 0.0f;
                view.DurationOverlay.color = Color.Lerp(CBuffClockLowColor, CBuffClockHighColor, normalized);
            }
        }
    }
    #endregion

    [System.Serializable]
    private sealed class SlotView
    {
        public string Name;
        public GameObject Root;
        public Image Background;
        public Image Icon;
        public Image CooldownOverlay;
        public TMP_Text KeyText;
    }

    [System.Serializable]
    private sealed class BuffView
    {
        public string Name;
        public GameObject Root;
        public Image Background;
        public Image Icon;
        public Image DurationOverlay;
    }
}
