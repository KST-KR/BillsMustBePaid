using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BILL-01-05] 스테이지 클리어 고지서 버튼 요약 표시
/// 납부 금액과 남은 기간을 버튼에 표시
/// 청구서 변경 이벤트에 맞춰 요약 갱신
/// </summary>
public class StageClearBillButtonController : MonoBehaviour
{
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private TMP_Text _periodText;
    [SerializeField] private Button _billButton;

    private BillManager _billManager;
    private CurrencyManager _currencyManager;
    private bool _isSubscribed;
    private bool _hasCachedButtonAppearance;
    private ColorBlock _defaultButtonColors;
    private Color _defaultButtonGraphicColor;
    private Color _defaultPeriodColor;

    private void OnEnable()
    {
        SubscribeEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        _billManager = BillManager.Instance;
        _currencyManager = CurrencyManager.Instance;

        if (_billManager != null)
        {
            _billManager.OnBillIssued += HandleBillChanged;
            _billManager.OnBillChanged += HandleBillChanged;
            _billManager.OnBillDueSoon += HandleBillChanged;
            _billManager.OnBillExpired += HandleBillChanged;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged += HandleBalanceChanged;
        }

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_billManager != null)
        {
            _billManager.OnBillIssued -= HandleBillChanged;
            _billManager.OnBillChanged -= HandleBillChanged;
            _billManager.OnBillDueSoon -= HandleBillChanged;
            _billManager.OnBillExpired -= HandleBillChanged;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        _isSubscribed = false;
    }

    private void HandleBillChanged(BillRecord bill)
    {
        Refresh();
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        if (type == ECurrencyType.Gold)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        CacheButtonAppearance();

        if (_billManager == null)
        {
            _billManager = BillManager.Instance;
        }

        BillRecord bill = _billManager != null ? _billManager.CurrentBill : null;
        bool hasExpiredBill = _billManager != null && _billManager.HasExpiredBills;
        if (bill == null && hasExpiredBill)
        {
            for (int i = _billManager.Bills.Count - 1; i >= 0; i--)
            {
                if (_billManager.Bills[i].Status == EBillStatus.Expired)
                {
                    bill = _billManager.Bills[i];
                    break;
                }
            }
        }

        if (bill == null && _billManager != null)
        {
            for (int i = _billManager.Bills.Count - 1; i >= 0; i--)
            {
                if (_billManager.Bills[i].Status == EBillStatus.Paid)
                {
                    bill = _billManager.Bills[i];
                    break;
                }
            }
        }

        bool hasPayableBill = bill != null && bill.Status == EBillStatus.Unpaid;
        bool canOpenBillPanel = bill != null;

        if (_amountText != null)
        {
            _amountText.text = bill != null ? "$" + bill.Amount.ToString("N0") : "$0";
        }

        if (_periodText != null)
        {
            int remainingDays = hasPayableBill ? bill.GetRemainingDays(_billManager.CurrentDay) : 0;
            if (hasPayableBill)
            {
                _periodText.text = remainingDays + "일 남음";
                _periodText.fontSize = 36.0f;
                _periodText.fontSizeMin = 20.0f;
                _periodText.fontSizeMax = 36.0f;
                _periodText.enableAutoSizing = true;
                _periodText.color = new Color(0.82f, 0.68f, 0.40f, 1.0f);
            }
            else if (bill != null && bill.Status == EBillStatus.Expired)
            {
                _periodText.text = "기한 만료";
                _periodText.fontSize = 36.0f;
                _periodText.enableAutoSizing = false;
                _periodText.color = new Color(0.95f, 0.24f, 0.24f, 1.0f);
            }
            else if (bill != null && bill.Status == EBillStatus.Paid)
            {
                int daysUntilNextBill = _billManager.GetDaysUntilNextBill(bill);
                _periodText.text = daysUntilNextBill >= 0
                    ? "다음 청구: " + daysUntilNextBill + "일"
                    : "납부 완료";
                _periodText.fontSize = 36.0f;
                _periodText.enableAutoSizing = true;
                _periodText.color = new Color(0.78f, 0.74f, 0.66f, 1.0f);
            }
            else
            {
                _periodText.text = "청구서 없음";
                _periodText.fontSize = 36.0f;
                _periodText.enableAutoSizing = false;
                _periodText.color = _defaultPeriodColor;
            }

            _periodText.enableWordWrapping = false;
            _periodText.overflowMode = TextOverflowModes.Truncate;
        }

        if (_billButton != null)
        {
            _billButton.interactable = canOpenBillPanel;
            SetPaidButtonAppearance(bill != null && bill.Status == EBillStatus.Paid);
        }
    }

    private void CacheButtonAppearance()
    {
        if (_hasCachedButtonAppearance)
        {
            return;
        }

        if (_billButton != null)
        {
            _defaultButtonColors = _billButton.colors;
            if (_billButton.targetGraphic != null)
            {
                _defaultButtonGraphicColor = _billButton.targetGraphic.color;
            }
        }

        if (_periodText != null)
        {
            _defaultPeriodColor = _periodText.color;
        }

        _hasCachedButtonAppearance = true;
    }

    private void SetPaidButtonAppearance(bool isPaid)
    {
        if (_billButton == null || _billButton.targetGraphic == null)
        {
            return;
        }

        if (!isPaid)
        {
            _billButton.colors = _defaultButtonColors;
            _billButton.targetGraphic.color = _defaultButtonGraphicColor;
            return;
        }

        Color neutralColor = new Color(0.22f, 0.21f, 0.19f, 1.0f);
        ColorBlock neutralColors = _defaultButtonColors;
        neutralColors.normalColor = Color.white;
        neutralColors.highlightedColor = Color.white;
        neutralColors.pressedColor = Color.white;
        neutralColors.selectedColor = Color.white;
        neutralColors.disabledColor = Color.white;
        neutralColors.colorMultiplier = 1.0f;
        _billButton.colors = neutralColors;
        _billButton.targetGraphic.color = neutralColor;
    }
}
