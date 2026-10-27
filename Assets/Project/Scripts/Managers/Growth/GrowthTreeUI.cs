using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [GROW-02-01] 스킬 트리 UI를 관리하는 클래스
/// GrowthManager의 업그레이드 데이터를 연결선, 스킬 노드, 상세 패널로 렌더링
/// </summary>
public class GrowthTreeUI : MonoBehaviour
{
    public static readonly Color ColorSlateGray = new Color(0.55f, 0.52f, 0.49f, 0.48f);
    public static readonly Color ColorLightGold = new Color(1.0f, 0.84f, 0.52f, 1.0f);
    public static readonly Color ColorAmber = new Color(0.88f, 0.62f, 0.25f, 0.98f);
    public static readonly Color ColorDeepBrown = new Color(0.38f, 0.25f, 0.18f, 0.82f);
    public static readonly Color ColorWoodBrown = new Color(0.40f, 0.27f, 0.16f, 0.85f);
    public static readonly Color ColorDeepGreen = new Color(0.15f, 0.31f, 0.25f, 1.0f);
    public static readonly Color ColorBurgundy = new Color(0.36f, 0.17f, 0.22f, 1.0f);
    public static readonly Color ColorDarkNavy = new Color(0.18f, 0.25f, 0.37f, 1.0f);
    public static readonly Color ColorDarkCharcoal = new Color(0.12f, 0.10f, 0.10f, 1.0f);
    public static readonly Color ColorBronzeGold = new Color(0.78f, 0.52f, 0.22f, 1.0f);

    private readonly Dictionary<string, Button> _nodeButtons = new Dictionary<string, Button>();
    private readonly List<Image> _connections = new List<Image>();
    private bool _hasUserSelectedUpgrade;

    #region 인스펙터
    [SerializeField] private GrowthManager _growthManager;
    [SerializeField] private RectTransform _treeRoot;
    [SerializeField] private RectTransform _nodeRoot;
    [SerializeField] private RectTransform _connecitonRoot;
    [SerializeField] private RectTransform _detailsRoot;
    [SerializeField] private Sprite _uiSprite;
    [SerializeField] private TMP_FontAsset _fontAsset;

    [SerializeField] private Vector2 _nodeSize = new Vector2(88.0f, 88.0f);

    [SerializeField] private TMP_Text _detailsText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _purchaseLabel;
    [SerializeField] private Image _detailsIcon;
    [SerializeField] private Button _purchaseButton;
    #endregion

    #region 이벤트
    private void OnEnable()
    {
        _hasUserSelectedUpgrade = false;
        if (_detailsRoot != null)
        {
            _detailsRoot.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (_growthManager == null)
        {
            _growthManager = GrowthManager.Instance;
        }

        if (_growthManager == null)
        {
            Debug.LogWarning("GrowthManager 확인");
            return;
        }

        BuildTree();

        _growthManager.OnSelectionChanged += HandleSelectionChanged;
        _growthManager.OnGrowthChanged += RefreshAll;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
        }

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (_growthManager != null)
        {
            _growthManager.OnSelectionChanged -= HandleSelectionChanged;
            _growthManager.OnGrowthChanged -= RefreshAll;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
        }
    }
    #endregion

    #region 커스텀 메소드 (Public)
    public void RefreshAll()
    {
        if (_growthManager == null)
        {
            return;
        }

        foreach (KeyValuePair<string, Button> pair in _nodeButtons)
        {
            UpgradeData upgrade = _growthManager.GetUpgrade(pair.Key);
            if (upgrade != null)
            {
                RefreshNode(upgrade, pair.Value);
            }
        }

        RefreshConnections();
        RefreshGold();
        if (_hasUserSelectedUpgrade)
        {
            RefreshDetails(_growthManager.SelectedUpgradeId);
        }
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void BuildTree()
    {
        if (_treeRoot == null)
        {
            _treeRoot = transform as RectTransform;
        }

        if (_nodeRoot == null)
        {
            _nodeRoot = _treeRoot;
        }

        if (_connecitonRoot == null)
        {
            _connecitonRoot = _nodeRoot;
        }

        if (_detailsRoot == null)
        {
            Transform detailsTransform = transform.Find("DetailsPanel");
            _detailsRoot = detailsTransform as RectTransform;
        }

        ClearChildren(_nodeRoot);
        if (_connecitonRoot != _nodeRoot)
        {
            ClearChildren(_connecitonRoot);
        }

        _nodeButtons.Clear();
        _connections.Clear();
        BindDetailsPanel();

        IReadOnlyList<UpgradeData> upgrades = _growthManager.Upgrades;
        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeData upgrade = upgrades[i];
            if (upgrade != null)
            {
                CreateConnections(upgrade, i);
            }
        }

        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeData upgrade = upgrades[i];
            if (upgrade != null)
            {
                CreateNode(upgrade);
            }
        }
    }

    private void BindDetailsPanel()
    {
        if (_detailsRoot == null)
        {
            return;
        }

        _detailsRoot.gameObject.SetActive(false);

        if (_detailsText == null)
        {
            Transform detailsText = _detailsRoot.Find("SelectedSkillDetails");
            _detailsText = detailsText != null ? detailsText.GetComponent<TMP_Text>() : null;
        }

        if (_goldText == null)
        {
            Transform goldText = _detailsRoot.Find("GoldBalance");
            _goldText = goldText != null ? goldText.GetComponent<TMP_Text>() : null;
        }

        if (_purchaseLabel == null)
        {
            Transform purchaseLabel = _detailsRoot.Find("PurchaseButton/Label");
            _purchaseLabel = purchaseLabel != null ? purchaseLabel.GetComponent<TMP_Text>() : null;
        }

        if (_detailsIcon == null)
        {
            Transform detailsIcon = _detailsRoot.Find("SelectedSkillIcon");
            _detailsIcon = detailsIcon != null ? detailsIcon.GetComponent<Image>() : null;
        }

        if (_purchaseButton == null)
        {
            Transform purchaseButton = _detailsRoot.Find("PurchaseButton");
            _purchaseButton = purchaseButton != null ? purchaseButton.GetComponent<Button>() : null;
        }

        if (_purchaseButton != null)
        {
            _purchaseButton.onClick.RemoveListener(PurchaseSelectedUpgrade);
            _purchaseButton.onClick.AddListener(PurchaseSelectedUpgrade);
        }
    }

    private void CreateNode(UpgradeData upgrade)
    {
        GameObject nodeObject = new GameObject("Node_" + upgrade.UpgradeId, typeof(RectTransform), typeof(Image), typeof(Button));
        nodeObject.transform.SetParent(_nodeRoot, false);
        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = upgrade.TreePosition;
        rect.sizeDelta = _nodeSize;

        Image background = nodeObject.GetComponent<Image>();
        background.sprite = _uiSprite;
        Button button = nodeObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        string upgradeId = upgrade.UpgradeId;
        button.onClick.AddListener(delegate { SelectNode(upgradeId); });

        //GameObject iconObject = new GameObject("Icon");
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(nodeObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(52.0f, 52.0f);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = upgrade.Icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject labelObject = CreateText("Name", nodeObject.transform, upgrade.DisplayName, 18, TextAlignmentOptions.Center, Color.white);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(-0.12f, 0.0f);
        labelRect.anchorMax = new Vector2(1.12f, 0.0f);
        labelRect.pivot = new Vector2(0.5f, 1.0f);
        labelRect.anchoredPosition = new Vector2(0.0f, -3.0f);
        labelRect.sizeDelta = new Vector2(20.0f, 34.0f);

        _nodeButtons.Add(upgradeId, button);
        RefreshNode(upgrade, button);
    }

    private void CreateConnections(UpgradeData upgrade, int skillIndex)
    {
        PrerequisiteUpgrade[] prerequisites = upgrade.PrerequisiteIds;
        for (int i = 0; i < prerequisites.Length; i++)
        {
            UpgradeData prerequisite = _growthManager.GetUpgrade(prerequisites[i].upgradeId);
            if (prerequisite != null)
            {
                CreateConnection(skillIndex, i, prerequisite.TreePosition, upgrade.TreePosition);
            }
        }
    }

    private void CreateConnection(int skillIndex, int lineIndex, Vector2 start, Vector2 end)
    {
        GameObject lineObject = new GameObject(GetConnectionName(skillIndex, lineIndex), typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(_connecitonRoot, false);
        lineObject.transform.SetAsFirstSibling();
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        Vector2 difference = end - start;

        //rect.anchorMin = new Vector2(0.3f, 0.7f);
        //rect.anchorMax = new Vector2(0.3f, 0.7f);
        //rect.pivot = new Vector2(0, 0.7f);

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.0f, 0.5f);
        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(difference.magnitude, 3.0f);

        //float angle = 0f;
        //if (difference.x > 0 && difference.y == 0) angle = 0f;
        //else if (difference.x > 0 && difference.y > 0) angle = 45f;
        //else if (difference.x == 0 && difference.y > 0) angle = 90f;
        //else if (difference.x < 0 && difference.y > 0) angle = 135f;
        //rect.localRotation = Quaternion.Euler(0, 0, angle);

        rect.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg);
        Image image = lineObject.GetComponent<Image>();
        image.sprite = _uiSprite;
        image.color = ColorWoodBrown;
        image.raycastTarget = false;
        _connections.Add(image);
    }

    private string GetConnectionName(int skillIndex, int lineIndex)
    {
        // S01_Line01~02
        return "S" + (skillIndex + 1).ToString("D2") + "_Line" + (lineIndex + 1).ToString("D2");
    }

    private void RefreshNode(UpgradeData upgrade, Button button)
    {
        EUpgradeNodeStatus status = _growthManager.GetNodeStatus(upgrade.UpgradeId);
        button.GetComponent<Image>().color = GetStatusColor(status, upgrade.TargetStat);
        TMP_Text label = button.transform.Find("Name").GetComponent<TMP_Text>();
        label.text = upgrade.DisplayName + "\n" + _growthManager.GetLevel(upgrade.UpgradeId) + "/" + upgrade.MaxLevel;
        Image icon = button.transform.Find("Icon").GetComponent<Image>();
        icon.sprite = upgrade.Icon;
        icon.enabled = upgrade.Icon != null;
        Color iconColor = Color.white;
        if (status == EUpgradeNodeStatus.Locked)
        {
            iconColor = ColorSlateGray;
        }
        else if (status == EUpgradeNodeStatus.Learned || status == EUpgradeNodeStatus.MaxLevel)
        {
            iconColor = ColorLightGold;
        }
        icon.color = iconColor;
    }

    private void RefreshConnections()
    {
        IReadOnlyList<UpgradeData> upgrades = _growthManager.Upgrades;
        int connectionIndex = 0;
        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeData upgrade = upgrades[i];
            PrerequisiteUpgrade[] prerequisites = upgrade.PrerequisiteIds;
            for (int j = 0; j < prerequisites.Length; j++)
            {
                if (connectionIndex >= _connections.Count)
                {
                    return;
                }

                _connections[connectionIndex++].color = _growthManager.IsLearned(prerequisites[j])
                    ? ColorAmber
                    : ColorDeepBrown;
            }
        }
    }

    private void RefreshDetails(string upgradeId)
    {
        if (_detailsRoot == null || _detailsText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(upgradeId))
        {
            _detailsRoot.gameObject.SetActive(false);
            if (_detailsIcon != null)
            {
                _detailsIcon.enabled = false;
            }
            if (_purchaseButton != null)
            {
                _purchaseButton.interactable = false;
                _purchaseButton.gameObject.SetActive(false);
            }
            return;
        }

        UpgradeData upgrade = _growthManager.GetUpgrade(upgradeId);
        if (upgrade == null)
        {
            _detailsRoot.gameObject.SetActive(false);
            return;
        }

        _detailsRoot.gameObject.SetActive(true);
        EUpgradeNodeStatus status = _growthManager.GetNodeStatus(upgradeId);
        int level = _growthManager.GetLevel(upgradeId);
        _detailsText.text =
            upgrade.DisplayName + "\n" +
            upgrade.Description + "\n\n레벨: " + level + "/" + upgrade.MaxLevel +
            "\n상태: " + GetStatusText(status) +
            "\n비용: " + upgrade.Cost +
            " Gold\n효과: " + GetStatText(upgrade.TargetStat) + " " +
            (upgrade.ValuePerLevel >= 0 ? "+" : string.Empty) + upgrade.ValuePerLevel +
            "\n누적 능력치: " + _growthManager.GetStatValue(upgrade.TargetStat) + GetPrerequisiteText(upgrade);

        if (_detailsIcon != null)
        {
            _detailsIcon.sprite = upgrade.Icon;
            _detailsIcon.enabled = upgrade.Icon != null;
        }

        bool available = status == EUpgradeNodeStatus.Unlocked || status == EUpgradeNodeStatus.Enhanceable;
        if (_purchaseButton != null)
        {
            _purchaseButton.gameObject.SetActive(available);
            _purchaseButton.interactable = available && _growthManager.CanPurchase(upgradeId);
        }
        if (_purchaseLabel != null)
        {
            _purchaseLabel.text = level > 0 ? "강화 - " + upgrade.Cost + " Gold" : "해금 - " + upgrade.Cost + " Gold";
        }
    }

    private void SelectNode(string upgradeId)
    {
        _hasUserSelectedUpgrade = true;
        _growthManager.SelectUpgrade(upgradeId);
        RefreshDetails(upgradeId);
    }

    private void HandleSelectionChanged(string upgradeId)
    {
        if (_hasUserSelectedUpgrade)
        {
            RefreshDetails(upgradeId);
        }
    }

    private string GetPrerequisiteText(UpgradeData upgrade)
    {
        if (upgrade.PrerequisiteIds.Length == 0)
        {
            return "\n\n선행 스킬: 없음";
        }

        string text = "\n\n선행 스킬:";
        for (int i = 0; i < upgrade.PrerequisiteIds.Length; i++)
        {
            PrerequisiteUpgrade prerequisite = upgrade.PrerequisiteIds[i];
            UpgradeData required = _growthManager.GetUpgrade(prerequisite.upgradeId);
            text += "\n· " + (required != null ? required.DisplayName : prerequisite.upgradeId) + " Lv." + prerequisite.requiredLevel;
        }

        return text;
    }

    private void PurchaseSelectedUpgrade()
    {
        if (_growthManager != null && _growthManager.SelectedUpgradeId != null)
        {
            _growthManager.PurchaseUpgrade(_growthManager.SelectedUpgradeId);
        }
    }

    private void HandleBalanceChanged(ECurrencyType type, int previous, int current)
    {
        if (type == ECurrencyType.Gold)
        {
            RefreshAll();
        }
    }

    private void RefreshGold()
    {
        if (_goldText != null)
        {
            _goldText.text = "GOLD  " + (CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0);
        }
    }

    private Color GetStatusColor(EUpgradeNodeStatus status, EUpgradeStat stat)
    {
        Color branchColor;
        switch (stat)
        {
            case EUpgradeStat.Stamina:
                branchColor = ColorDeepGreen;
                break;
            case EUpgradeStat.Luck:
                branchColor = ColorBurgundy;
                break;
            default:
                branchColor = ColorDarkNavy;
                break;
        }

        if (status == EUpgradeNodeStatus.Locked)
        {
            return Color.Lerp(branchColor, ColorDarkCharcoal, 0.48f);
        }

        if (status == EUpgradeNodeStatus.Learned || status == EUpgradeNodeStatus.MaxLevel)
        {
            return Color.Lerp(branchColor, ColorBronzeGold, 0.34f);
        }

        return branchColor;
    }

    private string GetStatusText(EUpgradeNodeStatus status)
    {
        switch (status)
        {
            case EUpgradeNodeStatus.Unlocked: return "해금 가능";
            case EUpgradeNodeStatus.Learned: return "학습 완료";
            case EUpgradeNodeStatus.Enhanceable: return "강화 가능";
            case EUpgradeNodeStatus.MaxLevel: return "최대 레벨";
            default: return "잠김 - 선행 스킬 필요";
        }
    }

    private string GetStatText(EUpgradeStat targetStat)
    {
        switch (targetStat)
        {
            case EUpgradeStat.Attack:
                return "공격력";
            case EUpgradeStat.Stamina:
                return "스태미나";
            case EUpgradeStat.AttackSpeed:
                return "공격 속도";
            case EUpgradeStat.AttackRange:
                return "공격 범위";
            case EUpgradeStat.CriticalChance:
                return "치명타 확률";
            case EUpgradeStat.Luck:
                return "행운";
            default:
                return "알 수 없음";
        }
    }

    private GameObject CreateText(string name, Transform parent, string value, int size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = _fontAsset != null ? _fontAsset : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return textObject;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
    #endregion
}