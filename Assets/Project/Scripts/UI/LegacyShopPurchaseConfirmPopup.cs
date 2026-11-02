using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레거시 상점의 구매 여부 확인 창을 표시하고 구매 결과를 알림
/// </summary>
public sealed class LegacyShopPurchaseConfirmPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private TMP_Text _confirmButtonText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private LegacyShopRingHover _pendingItem;
    public bool IsOpen => gameObject.activeInHierarchy;

    private void Awake()
    {
        BindButtons();
    }

    public void Initialize(TMP_Text titleText, TMP_Text descriptionText, TMP_Text statusText,
        TMP_Text confirmButtonText, Button confirmButton, Button cancelButton)
    {
        _titleText = titleText;
        _descriptionText = descriptionText;
        _statusText = statusText;
        _confirmButtonText = confirmButtonText;
        _confirmButton = confirmButton;
        _cancelButton = cancelButton;
        BindButtons();
    }

    private void BindButtons()
    {
        if (_confirmButton == null || _cancelButton == null) return;
        _confirmButton.onClick.RemoveListener(ConfirmPurchase);
        _confirmButton.onClick.AddListener(ConfirmPurchase);
        _cancelButton.onClick.RemoveListener(Cancel);
        _cancelButton.onClick.AddListener(Cancel);
    }

    public void Open(LegacyShopRingHover item, string title, string description, string price)
    {
        if (item == null || _titleText == null || _descriptionText == null || _statusText == null ||
            _confirmButtonText == null || _confirmButton == null)
        {
            Debug.LogError("구매 확인 창의 아이템 또는 UI 참조가 누락되었습니다.", this);
            return;
        }

        _pendingItem = item;
        _titleText.text = "구매 확인";
        _descriptionText.text = title + "\n\n" + description + "\n\n가격: " + price + " 포인트";
        _statusText.text = "구매하시겠습니까?";
        _confirmButtonText.text = "Yes";
        _confirmButton.interactable = true;
        gameObject.SetActive(true);
    }

    private void ConfirmPurchase()
    {
        if (_pendingItem == null)
        {
            _statusText.text = "구매할 아이템을 찾을 수 없습니다";
            return;
        }

        LegacyShopRingHover confirmedItem = _pendingItem;
        if (confirmedItem.TryPurchase(out string result))
        {
            _pendingItem = null;
            gameObject.SetActive(false);
            return;
        }

        _pendingItem = confirmedItem;
        _statusText.text = result;
    }

    private void Cancel()
    {
        _pendingItem = null;
        gameObject.SetActive(false);
    }
}
