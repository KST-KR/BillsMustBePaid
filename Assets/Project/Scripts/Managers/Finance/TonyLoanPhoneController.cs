using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대출 빅토니 연결 UI 컨트롤러
/// </summary>
public class TonyLoanPhoneController : MonoBehaviour
{
    [SerializeField] private Button _callTonyButton;
    [SerializeField] private GameObject _callPanel;
    [SerializeField] private CanvasGroup _callCanvasGroup;
    [SerializeField] private TMP_Text _callTitleText;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private TMP_Text _loanAmountText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private TMP_Text _acceptButtonText;
    [SerializeField] private Button _hangUpButton;
    [SerializeField] private TMP_Text _hangUpButtonText;
    [SerializeField] private GameObject _phoneRootObject;
    [SerializeField, Min(1)] private int _repaymentDays = 5;
    [SerializeField, Range(1, 200)] private int _repaymentPercent = 125;

    private BillManager _billManager;
    private CurrencyManager _currencyManager;
    private Coroutine _callSequence;
    private int _offeredPrincipal;
    private bool _pausedGameForCall;
    private bool _isConnected;
    private float _previousTimeScale = 1.0f;

    private void Awake()
    {
        _billManager = BillManager.Instance;
        _currencyManager = CurrencyManager.Instance;
        if (_callPanel != null)
        {
            _callPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RefreshCallButton();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        if (_callSequence != null)
        {
            StopCoroutine(_callSequence);
            _callSequence = null;
        }

        if (_callPanel != null)
        {
            _callPanel.SetActive(false);
        }

        HidePhoneModel();
        RestoreGamePause();
    }

    private void OnDestroy()
    {
        HidePhoneModel();
    }

    private void SubscribeEvents()
    {
        _billManager = BillManager.Instance;
        _currencyManager = CurrencyManager.Instance;
        if (_billManager != null)
        {
            _billManager.OnBillChanged += HandleBillChanged;
            _billManager.OnBillIssued += HandleBillChanged;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged += HandleBalanceChanged;
        }

        if (_callTonyButton != null)
        {
            _callTonyButton.onClick.AddListener(StartCall);
        }

        if (_acceptButton != null)
        {
            _acceptButton.onClick.AddListener(AcceptLoan);
        }

        if (_hangUpButton != null)
        {
            _hangUpButton.onClick.AddListener(HangUp);
        }
    }

    private void UnsubscribeEvents()
    {
        if (_billManager != null)
        {
            _billManager.OnBillChanged -= HandleBillChanged;
            _billManager.OnBillIssued -= HandleBillChanged;
        }

        if (_currencyManager != null)
        {
            _currencyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        if (_callTonyButton != null)
        {
            _callTonyButton.onClick.RemoveListener(StartCall);
        }

        if (_acceptButton != null)
        {
            _acceptButton.onClick.RemoveListener(AcceptLoan);
        }

        if (_hangUpButton != null)
        {
            _hangUpButton.onClick.RemoveListener(HangUp);
        }
    }

    private void HandleBillChanged(BillRecord bill)
    {
        RefreshCallButton();
    }

    private void HandleBalanceChanged(ECurrencyType type, int previousAmount, int currentAmount)
    {
        if (type == ECurrencyType.Gold)
        {
            RefreshCallButton();
        }
    }

    private void RefreshCallButton()
    {
        if (_callTonyButton != null && _billManager != null)
        {
            _callTonyButton.gameObject.SetActive(_billManager.CanTakeTonyLoan);
        }
    }

    private void StartCall()
    {
        if (_billManager == null || !_billManager.CanTakeTonyLoan || _callPanel == null)
        {
            RefreshCallButton();
            return;
        }

        _offeredPrincipal = _billManager.GetTonyLoanAmount();
        if (_offeredPrincipal <= 0)
        {
            RefreshCallButton();
            return;
        }

        _isConnected = false;
        _callPanel.SetActive(true);
        _callCanvasGroup.alpha = 0.0f;
        _callTitleText.text = "빅 토니에게 전화 연결 중";
        _dialogueText.text = "뚜르르르...";
        _loanAmountText.text = string.Empty;
        _acceptButton.gameObject.SetActive(false);
        _hangUpButton.gameObject.SetActive(true);
        _hangUpButtonText.text = "전화 끊기";

        if (Time.timeScale > 0.0f)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0.0f;
            _pausedGameForCall = true;
        }

        ShowPhoneModel();
        _callSequence = StartCoroutine(ConnectCall());
    }

    private IEnumerator ConnectCall()
    {
        float elapsed = 0.0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            _callCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.35f);

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.75f);
        if (_callPanel == null || !_callPanel.activeInHierarchy)
        {
            yield break;
        }

        _isConnected = true;
        int repaymentAmount = CalculateRepayment(_offeredPrincipal);
        _callTitleText.text = "빅 토니";
        _dialogueText.text = "이번 청구서가 급하다고? 필요한 돈을 빌려주지\n대신 " + _repaymentDays + "일 안에 원금의 " + Mathf.Max(0, _repaymentPercent - 100) + "%를 더해 갚아";
        int penaltyPercent = Mathf.RoundToInt(BillManager.CTonyLoanPenaltyRate * 100.0f);
        _loanAmountText.text = "빌릴 금액  $" + _offeredPrincipal.ToString("N0") + "\n상환 금액  $" + repaymentAmount.ToString("N0") + " - " + _repaymentDays + "일 후";
        _acceptButton.gameObject.SetActive(true);
        _callSequence = null;
    }

    private void AcceptLoan()
    {
        if (!_isConnected || _billManager == null || !_billManager.TryCreateTonyLoan(_offeredPrincipal, _repaymentDays))
        {
            _dialogueText.text = "지금은 대출을 진행할 수 없음\n청구서와 잔고를 다시 확인해";
            _acceptButton.gameObject.SetActive(false);
            return;
        }

        _isConnected = false;
        _callTitleText.text = "대출 완료";
        _dialogueText.text = "잘 써라\n약속한 날짜에 꼭 갚도록 해";
        _loanAmountText.text = "$" + _offeredPrincipal.ToString("N0") + " 입금 완료 - " + _repaymentDays + "일 후 $" + CalculateRepayment(_offeredPrincipal).ToString("N0") + " 상환";
        _acceptButton.gameObject.SetActive(false);
        _hangUpButtonText.text = "확인";
        RefreshCallButton();
    }

    private void HangUp()
    {
        if (!_isConnected && _callSequence != null)
        {
            StopCoroutine(_callSequence);
            _callSequence = null;
        }

        if (_callPanel != null)
        {
            _callPanel.SetActive(false);
        }

        HidePhoneModel();

        _isConnected = false;
        RestoreGamePause();
        RefreshCallButton();
    }

    private void RestoreGamePause()
    {
        if (_pausedGameForCall)
        {
            Time.timeScale = _previousTimeScale;
        }

        _pausedGameForCall = false;
    }

    private void ShowPhoneModel()
    {
        if (_phoneRootObject == null)
        {
            return;
        }
        _phoneRootObject.SetActive(true);
    }

    private void HidePhoneModel()
    {
        if (_phoneRootObject != null)
        {
            _phoneRootObject.SetActive(false);
        }
    }

    private int CalculateRepayment(int principal)
    {
        long amount = ((long)principal * Mathf.Max(1, _repaymentPercent) + 99L) / 100L;
        return amount > int.MaxValue ? int.MaxValue : (int)amount;
    }
}
