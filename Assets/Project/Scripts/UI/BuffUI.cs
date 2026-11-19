using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffUI : MonoBehaviour
{
    #region ÀÎ½ºÆåÅÍ
    [Header("½ºÅ×ÀÌÁö Å¬¸®¾î ÆÐ³Î (È°¼ºÈ­ ½Ã ¹öÇÁ UI ÀÚµ¿ ¼û±è)")]
    [SerializeField] private GameObject _stageClearPanel;

    [Header("ÀèÆÌ È®·ü 5¹è UI")]
    [SerializeField] private GameObject _jackpotBuffRoot;
    [SerializeField] private TextMeshProUGUI _jackpotBuffText;
    [SerializeField] private Image _jackpotFillBar;

    [Header("ÄÚÀÎ Å‰µæ·® 2¹è UI")]
    [SerializeField] private GameObject _doubleCoinBuffRoot;
    [SerializeField] private TextMeshProUGUI _doubleCoinBuffText;
    [SerializeField] private Image _doubleCoinFillBar;
    #endregion

    #region ÀÌº¥Æ® ÇÔ¼ö
    private void Start()
    {
        if (BuffManager.HasInstance)
        {
            BuffManager.Instance.OnJackpotBuffStateChanged += HandleJackpotStateChanged;
            BuffManager.Instance.OnDoubleCoinBuffStateChanged += HandleDoubleCoinStateChanged;

            // ½ÃÀÛ ½Ã ÇöÀç ¹öÇÁ »óÅÂ¿Í UI µ¿±âÈ­
            HandleJackpotStateChanged(BuffManager.Instance.IsJackpotBuffActive);
            HandleDoubleCoinStateChanged(BuffManager.Instance.IsDoubleCoinBuffActive);
        }
        else
        {
            if (_jackpotBuffRoot != null) _jackpotBuffRoot.SetActive(false);
            if (_doubleCoinBuffRoot != null) _doubleCoinBuffRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (BuffManager.HasInstance)
        {
            BuffManager.Instance.OnJackpotBuffStateChanged -= HandleJackpotStateChanged;
            BuffManager.Instance.OnDoubleCoinBuffStateChanged -= HandleDoubleCoinStateChanged;
        }
    }

    private void Update()
    {
        if (_stageClearPanel != null && _stageClearPanel.activeInHierarchy)
        {
            HideAllBuffUI();
            return;
        }

        if (!BuffManager.HasInstance)
        {
            return;
        }

        // 1. ÀèÆÌ È®·ü 5¹è ¹öÇÁ °»½Å
        if (BuffManager.Instance.IsJackpotBuffActive)
        {
            if (_jackpotBuffText != null)
            {
                _jackpotBuffText.text = $"ÀèÆÌ È®·ü {BuffManager.Instance.JackpotChanceMultiplier:0}¹è";
            }

            if (_jackpotFillBar != null)
            {
                _jackpotFillBar.fillAmount = BuffManager.Instance.JackpotProgress;
            }
        }

        // 2. ÄÚÀÎ È¹µæ·® 2¹è ¹öÇÁ °»½Å
        if (BuffManager.Instance.IsDoubleCoinBuffActive)
        {
            if (_doubleCoinBuffText != null)
            {
                _doubleCoinBuffText.text = $"ÄÚÀÎ È¹µæ {BuffManager.Instance.CoinGainMultiplier:0}¹è";
            }

            if (_doubleCoinFillBar != null)
            {
                _doubleCoinFillBar.fillAmount = BuffManager.Instance.DoubleCoinProgress;
            }
        }
    }
    #endregion

    #region Ä¿½ºÅÒ ÇÔ¼ö (Private)
    private void HandleJackpotStateChanged(bool isActive)
    {
        if (_stageClearPanel != null && _stageClearPanel.activeInHierarchy)
        {
            isActive = false;
        }

        if (_jackpotBuffRoot != null)
        {
            _jackpotBuffRoot.SetActive(isActive);
        }

        if (isActive && _jackpotBuffText != null && BuffManager.HasInstance)
        {
            _jackpotBuffText.text = $"ÀèÆÌ È®·ü {BuffManager.Instance.JackpotChanceMultiplier:0}¹è";
        }
    }

    private void HandleDoubleCoinStateChanged(bool isActive)
    {
        if (_stageClearPanel != null && _stageClearPanel.activeInHierarchy)
        {
            isActive = false;
        }

        if (_doubleCoinBuffRoot != null)
        {
            _doubleCoinBuffRoot.SetActive(isActive);
        }

        if (isActive && _doubleCoinBuffText != null && BuffManager.HasInstance)
        {
            _doubleCoinBuffText.text = $"ÄÚÀÎ È¹µæ {BuffManager.Instance.CoinGainMultiplier:0}¹è";
        }
    }

    private void HideAllBuffUI()
    {
        if (_jackpotBuffRoot != null && _jackpotBuffRoot.activeSelf)
        {
            _jackpotBuffRoot.SetActive(false);
        }

        if (_doubleCoinBuffRoot != null && _doubleCoinBuffRoot.activeSelf)
        {
            _doubleCoinBuffRoot.SetActive(false);
        }
    }
    #endregion
}
