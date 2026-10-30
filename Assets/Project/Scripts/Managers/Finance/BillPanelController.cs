using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BILL-01-04][BILL-01-05][BILL-02-01] 청구서 패널과 납부 기한 UI 관리
/// 현재 청구서 정보와 납부 완료 도장 표시
/// 기한 만료 시 파산 처리 버튼 노출
/// </summary>
public class BillPanelController : MonoBehaviour
{
    #region 인스펙터
    [Header("청구서 내용")]
    [SerializeField] private TMP_Text _categoryText;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _noticeText;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private TMP_Text _dueLabelText;
    [SerializeField] private TMP_Text _dueText;
    [SerializeField] private TMP_Text _footerText;
    [SerializeField] private Button _payButton;
    [SerializeField] private Image _paidStampImage;
    [SerializeField] private TMP_Text _paidStampText;

    [Header("파산 처리")]
    [SerializeField] private GameObject _bankruptcyButtonObject;
    [SerializeField] private Button _bankruptcyButton; 
    #endregion

    private BillManager _billManager;
    private CurrencyManager _currencyManager;
    private BillRecord _displayedBill;
    private bool _isSubscribed;
    private bool _hasCachedDueColors;
    private Color _defaultDueLabelColor;
    private Color _defaultDueValueColor;

    #region 이벤트 함수
    private void OnEnable()
    {
        SubscribeEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    } 
    #endregion

    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (_billManager == null)
        {
            _billManager = BillManager.Instance;
        }

        _currencyManager = CurrencyManager.Instance;

        if (_billManager != null)
        {
            _billManager.OnBillIssued += HandleBillChanged;
            _billManager.OnBillChanged += HandleBillChanged;
            _billManager.OnBillPaid += HandleBillChanged;
            _billManager.OnBillDueSoon += HandleBillChanged;
            _billManager.OnBillExpired += HandleBillChanged;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged += HandleBalanceChanged;
        }

        if (_payButton != null)
        {
            _payButton.onClick.AddListener(HandlePayClicked);
        }

        if (_bankruptcyButton != null)
        {
            _bankruptcyButton.onClick.AddListener(HandleBankruptcyClicked);
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
            _billManager.OnBillPaid -= HandleBillChanged;
            _billManager.OnBillDueSoon -= HandleBillChanged;
            _billManager.OnBillExpired -= HandleBillChanged;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        if (_payButton != null)
        {
            _payButton.onClick.RemoveListener(HandlePayClicked);
        }

        if (_bankruptcyButton != null)
        {
            _bankruptcyButton.onClick.RemoveListener(HandleBankruptcyClicked);
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

    private void HandlePayClicked()
    {
        if (_billManager == null || _displayedBill == null || _displayedBill.Status != EBillStatus.Unpaid)
        {
            return;
        }

        _billManager.TryPayBill(_displayedBill.BillId);
    }

    private void HandleBankruptcyClicked()
    {
        if (_billManager == null || !_billManager.HasExpiredBills || RoguelikeManager.Instance == null)
        {
            return;
        }

        RoguelikeManager.Instance.RestartAfterBankruptcy();
    }

    private void Refresh()
    {
        if (_billManager == null)
        {
            _billManager = BillManager.Instance;
        }

        if (_billManager == null)
        {
            DisplayBill(null);
            SetBankruptcyButtonVisible(false);
            return;
        }

        BillRecord bill = _billManager.HasExpiredBills
            ? FindMostRecentBill(EBillStatus.Expired)
            : _billManager.CurrentBill;
        if (bill == null)
        {
            bill = FindMostRecentBill(EBillStatus.Paid);
        }

        if (bill == null && _billManager.HasExpiredBills)
        {
            bill = FindMostRecentBill(EBillStatus.Expired);
        }

        DisplayBill(bill);
        SetBankruptcyButtonVisible(_billManager.HasExpiredBills);
    }

    private BillRecord FindMostRecentBill(EBillStatus status)
    {
        for (int i = _billManager.Bills.Count - 1; i >= 0; i--)
        {
            if (_billManager.Bills[i].Status == status)
            {
                return _billManager.Bills[i];
            }
        }

        return null;
    }

    private void DisplayBill(BillRecord bill)
    {
        _displayedBill = bill;
        bool hasBill = bill != null;
        bool isPaid = hasBill && bill.Status == EBillStatus.Paid;
        bool isExpired = hasBill && bill.Status == EBillStatus.Expired;
        CacheDueColors();

        if (_categoryText != null)
        {
            _categoryText.text = hasBill ? bill.Category : string.Empty;
        }

        if (_titleText != null)
        {
            _titleText.text = hasBill ? bill.BillName : "청구서 없음";
        }

        if (_noticeText != null)
        {
            _noticeText.text = isExpired
                ? "납부 기한이 지나 파산 위험에 처했습니다"
                : hasBill ? "청구서를 기한 내에 납부해 주세요" : string.Empty;
        }

        if (_amountText != null)
        {
            _amountText.text = hasBill ? "$" + bill.Amount.ToString("N0") : "$0";
        }

        if (_dueLabelText != null)
        {
            _dueLabelText.text = isExpired ? "연체 수수료" : isPaid ? "다음 청구까지" : "납부 기한";
            _dueLabelText.color = isExpired
                ? new Color(0.95f, 0.16f, 0.16f, 1.0f)
                : _defaultDueLabelColor;
        }

        if (_dueText != null)
        {
            if (isExpired && _billManager != null)
            {
                float rate = bill.GetPenaltyRate(_billManager.CurrentDay);
                int percentage = Mathf.RoundToInt(Mathf.Clamp01(rate) * 100f);
                _dueText.text = percentage + "%";
                _dueText.color = new Color(0.95f, 0.16f, 0.16f, 1.0f);
            }
            else
            {
                if (isPaid && _billManager != null)
                {
                    int daysUntilNextBill = _billManager.GetDaysUntilNextBill(bill);
                    _dueText.text = daysUntilNextBill >= 0 ? daysUntilNextBill + "일" : "완료";
                }
                else
                {
                    int remainingDays = hasBill && _billManager != null
                        ? bill.GetRemainingDays(_billManager.CurrentDay)
                        : 0;
                    _dueText.text = hasBill ? remainingDays + "일" : "-";
                }

                _dueText.color = _defaultDueValueColor;
            }
        }

        if (_footerText != null)
        {
            _footerText.text = isPaid ? "납부 완료" : isExpired ? "기한 만료" : hasBill ? "즉시 납부 바랍니다" : string.Empty;
        }

        if (_payButton != null)
        {
            bool bankruptcyPending = _billManager != null && _billManager.HasExpiredBills;
            _payButton.gameObject.SetActive(hasBill && !isPaid && !isExpired && !bankruptcyPending);
            int balance = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
            _payButton.interactable = hasBill && !isPaid && !isExpired && !bankruptcyPending && balance >= bill.Amount;
        }

        if (_paidStampImage != null)
        {
            _paidStampImage.gameObject.SetActive(isPaid);
        }

        if (_paidStampText != null)
        {
            _paidStampText.gameObject.SetActive(isPaid);
        }
    }

    private void CacheDueColors()
    {
        if (_hasCachedDueColors)
        {
            return;
        }

        if (_dueLabelText != null)
        {
            _defaultDueLabelColor = _dueLabelText.color;
        }

        if (_dueText != null)
        {
            _defaultDueValueColor = _dueText.color;
        }

        _hasCachedDueColors = true;
    }

    private void SetBankruptcyButtonVisible(bool isVisible)
    {
        if (_bankruptcyButtonObject != null)
        {
            _bankruptcyButtonObject.SetActive(isVisible);
        }
    }
}
