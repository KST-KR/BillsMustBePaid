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
    [SerializeField] private Camera _phoneRenderCamera;
    [SerializeField] private RawImage _phoneRenderImage;
    [SerializeField] private AudioClip _phoneOpenSound;
    [SerializeField] private AudioClip _phoneCloseSound;
    [SerializeField, Min(1)] private int _repaymentDays = 5;
    [SerializeField, Range(1, 200)] private int _repaymentPercent = 125;
    [SerializeField] private Vector3 _phoneCameraPosition = new Vector3(0.58f, 0.0f, 0.65f);
    [SerializeField] private Vector3 _phoneCameraRotation = new Vector3(90.0f, 0.0f, -8.0f);
    [SerializeField, Min(0.1f)] private float _phoneScale = 0.45f;

    private BillManager _billManager;
    private CurrencyManager _currencyManager;
    private AudioSource _audioSource;
    private Transform _phoneRoot;
    private Coroutine _callSequence;
    private int _offeredPrincipal;
    private bool _pausedGameForCall;
    private bool _isConnected;
    private float _previousTimeScale = 1.0f;
    private RenderTexture _phoneRenderTexture;
    private Camera _mainCamera;
    private int _previousMainCameraMask;
    private RectTransform _phoneImageRect;
    private Vector2 _phoneImagePosition;

    private void Awake()
    {
        _billManager = BillManager.Instance;
        _currencyManager = CurrencyManager.Instance;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0.0f;
        _mainCamera = Camera.main;
        _phoneImageRect = _phoneRenderImage != null ? _phoneRenderImage.rectTransform : null;
        if (_phoneRenderCamera != null && _phoneRenderImage != null)
        {
            _phoneRenderTexture = new RenderTexture(512, 768, 24, RenderTextureFormat.ARGB32);
            _phoneRenderTexture.name = "TonyLoanPhoneRenderTexture";
            _phoneRenderTexture.Create();
            _phoneRenderCamera.targetTexture = _phoneRenderTexture;
            _phoneRenderImage.texture = _phoneRenderTexture;
            _phoneRenderCamera.enabled = false;
            _phoneImagePosition = _phoneImageRect.anchoredPosition;
        }

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
        if (_phoneRenderCamera != null)
        {
            _phoneRenderCamera.targetTexture = null;
        }

        if (_phoneRenderTexture != null)
        {
            _phoneRenderTexture.Release();
            Destroy(_phoneRenderTexture);
        }
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
        if (_audioSource != null && _phoneOpenSound != null)
        {
            _audioSource.PlayOneShot(_phoneOpenSound);
        }

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
            if (_phoneRoot != null)
            {
                float progress = Mathf.Clamp01(elapsed / 0.35f);
                _phoneRoot.localScale = Vector3.one * (_phoneScale * progress);
                _phoneRoot.localPosition = _phoneCameraPosition + Vector3.right * (0.75f * (1.0f - progress));
                UpdatePhoneRenderCamera();
            }

            if (_phoneImageRect != null)
            {
                float progress = Mathf.Clamp01(elapsed / 0.35f);
                _phoneImageRect.anchoredPosition = _phoneImagePosition + Vector2.right * (300.0f * (1.0f - progress));
            }

            yield return null;
        }

        if (_phoneRoot != null)
        {
            _phoneRoot.localScale = Vector3.one * _phoneScale;
            _phoneRoot.localPosition = _phoneCameraPosition;
            UpdatePhoneRenderCamera();
        }

        if (_phoneImageRect != null)
        {
            _phoneImageRect.anchoredPosition = _phoneImagePosition;
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
        _loanAmountText.text = "빌릴 금액  $" + _offeredPrincipal.ToString("N0") + "\n상환 금액  $" + repaymentAmount.ToString("N0") + "  ·  " + _repaymentDays + "일 후\n연체 시 스테이지 수입 " + penaltyPercent + "%부터 차감, 연체 기간에 따라 증가";
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
        _loanAmountText.text = "$" + _offeredPrincipal.ToString("N0") + " 입금 완료  ·  " + _repaymentDays + "일 후 $" + CalculateRepayment(_offeredPrincipal).ToString("N0") + " 상환";
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

        if (_audioSource != null && _phoneCloseSound != null)
        {
            _audioSource.PlayOneShot(_phoneCloseSound);
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

        _phoneRoot = _phoneRootObject.transform;
        _phoneRoot.localPosition = _phoneCameraPosition + Vector3.right * 0.75f;
        _phoneRoot.localRotation = Quaternion.Euler(_phoneCameraRotation);
        _phoneRoot.localScale = Vector3.zero;
        _phoneRootObject.SetActive(true);
        if (_phoneRenderImage != null)
        {
            _phoneRenderImage.gameObject.SetActive(true);
        }

        if (_phoneRenderCamera != null)
        {
            _phoneRenderCamera.enabled = true;
        }

        if (_mainCamera != null)
        {
            _previousMainCameraMask = _mainCamera.cullingMask;
            _mainCamera.cullingMask &= ~(1 << 31);
        }
    }

    private void HidePhoneModel()
    {
        if (_phoneRootObject != null)
        {
            _phoneRootObject.SetActive(false);
        }

        if (_phoneRenderCamera != null)
        {
            _phoneRenderCamera.enabled = false;
        }

        if (_phoneRenderImage != null)
        {
            _phoneRenderImage.gameObject.SetActive(false);
        }

        if (_mainCamera != null)
        {
            _mainCamera.cullingMask = _previousMainCameraMask;
        }

        _phoneRoot = null;
    }

    private void UpdatePhoneRenderCamera()
    {
        if (_phoneRenderCamera == null || _phoneRootObject == null)
        {
            return;
        }

        Renderer[] renderers = _phoneRootObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 forward = _mainCamera != null ? _mainCamera.transform.forward : Vector3.forward;
        Vector3 up = _mainCamera != null ? _mainCamera.transform.up : Vector3.up;
        float distance = Mathf.Max(1.0f, bounds.extents.magnitude * 2.8f);
        _phoneRenderCamera.transform.position = bounds.center - forward * distance;
        _phoneRenderCamera.transform.rotation = Quaternion.LookRotation(forward, up);
        _phoneRenderCamera.fieldOfView = 38.0f;
        _phoneRenderCamera.nearClipPlane = 0.01f;
        _phoneRenderCamera.farClipPlane = distance * 3.0f;
    }

    private int CalculateRepayment(int principal)
    {
        long amount = ((long)principal * Mathf.Max(1, _repaymentPercent) + 99L) / 100L;
        return amount > int.MaxValue ? int.MaxValue : (int)amount;
    }
}
