using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BuffUI : MonoBehaviour
{
    #region ÀÎ½ºÆåÅÍ
    [Header("ÀèÆÌ È®·ü 5¹è UI")]
    [SerializeField] private GameObject _jackpotBuffRoot;
    [SerializeField] private TextMeshProUGUI _jackpotBuffText;
    [SerializeField] private Image _jackpotFillBar;

    [Header("ÄÚÀÎ Å‰µæ·® 2¹è UI")]
    [SerializeField] private GameObject _doubleCoinBuffRoot;
    [SerializeField] private TextMeshProUGUI _doubleCoinBuffText;
    [SerializeField] private Image _doubleCoinFillBar;
    #endregion

    #region ³»ºÎ º¯¼ö
    private Coroutine _jackpotUICoroutine;
    private Coroutine _doubleCoinUICoroutine;
    #endregion

    #region ÀÌº¥Æ® ÇÔ¼ö
    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared += HandleStageCleared;
        }

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
            HideAllBuffUI();
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
        }

        if (BuffManager.HasInstance)
        {
            BuffManager.Instance.OnJackpotBuffStateChanged -= HandleJackpotStateChanged;
            BuffManager.Instance.OnDoubleCoinBuffStateChanged -= HandleDoubleCoinStateChanged;
        }
    }
    #endregion

    #region Ä¿½ºÅÒ ÇÔ¼ö (Private)
    private void HandleStageCleared(int stageNumber)
    {
        HideAllBuffUI();
    }

    private void HandleJackpotStateChanged(bool isActive)
    {
        if (StageManager.Instance != null && !StageManager.Instance.IsStageActive)
        {
            isActive = false;
        }

        if (_jackpotUICoroutine != null)
        {
            StopCoroutine(_jackpotUICoroutine);
            _jackpotUICoroutine = null;
        }

        if (_jackpotBuffRoot != null)
        {
            _jackpotBuffRoot.SetActive(isActive);
        }

        if (isActive && BuffManager.HasInstance)
        {
            if (_jackpotBuffText != null)
            {
                _jackpotBuffText.text = $"ÀèÆÌ È®·ü {BuffManager.Instance.JackpotChanceMultiplier:0}¹è";
            }

            if (_jackpotFillBar != null)
            {
                _jackpotFillBar.fillAmount = BuffManager.Instance.JackpotProgress;
            }

            _jackpotUICoroutine = StartCoroutine(UpdateJackpotUIRoutine());
        }
        else if (_jackpotFillBar != null)
        {
            _jackpotFillBar.fillAmount = 0f;
        }
    }

    private void HandleDoubleCoinStateChanged(bool isActive)
    {
        if (StageManager.Instance != null && !StageManager.Instance.IsStageActive)
        {
            isActive = false;
        }

        if (_doubleCoinUICoroutine != null)
        {
            StopCoroutine(_doubleCoinUICoroutine);
            _doubleCoinUICoroutine = null;
        }

        if (_doubleCoinBuffRoot != null)
        {
            _doubleCoinBuffRoot.SetActive(isActive);
        }

        if (isActive && BuffManager.HasInstance)
        {
            if (_doubleCoinBuffText != null)
            {
                _doubleCoinBuffText.text = $"ÄÚÀÎ È¹µæ {BuffManager.Instance.CoinGainMultiplier:0}¹è";
            }

            if (_doubleCoinFillBar != null)
            {
                _doubleCoinFillBar.fillAmount = BuffManager.Instance.DoubleCoinProgress;
            }

            _doubleCoinUICoroutine = StartCoroutine(UpdateDoubleCoinUIRoutine());
        }
        else if (_doubleCoinFillBar != null)
        {
            _doubleCoinFillBar.fillAmount = 0f;
        }
    }

    private IEnumerator UpdateJackpotUIRoutine()
    {
        while (BuffManager.HasInstance && BuffManager.Instance.IsJackpotBuffActive)
        {
            if (_jackpotFillBar != null)
            {
                _jackpotFillBar.fillAmount = BuffManager.Instance.JackpotProgress;
            }

            yield return null;
        }

        if (_jackpotFillBar != null)
        {
            _jackpotFillBar.fillAmount = 0f;
        }

        if (_jackpotBuffRoot != null && _jackpotBuffRoot.activeSelf)
        {
            _jackpotBuffRoot.SetActive(false);
        }

        _jackpotUICoroutine = null;
    }

    private IEnumerator UpdateDoubleCoinUIRoutine()
    {
        while (BuffManager.HasInstance && BuffManager.Instance.IsDoubleCoinBuffActive)
        {
            if (_doubleCoinFillBar != null)
            {
                _doubleCoinFillBar.fillAmount = BuffManager.Instance.DoubleCoinProgress;
            }

            yield return null;
        }

        if (_doubleCoinFillBar != null)
        {
            _doubleCoinFillBar.fillAmount = 0f;
        }

        if (_doubleCoinBuffRoot != null && _doubleCoinBuffRoot.activeSelf)
        {
            _doubleCoinBuffRoot.SetActive(false);
        }

        _doubleCoinUICoroutine = null;
    }

    private void HideAllBuffUI()
    {
        if (_jackpotUICoroutine != null)
        {
            StopCoroutine(_jackpotUICoroutine);
            _jackpotUICoroutine = null;
        }

        if (_doubleCoinUICoroutine != null)
        {
            StopCoroutine(_doubleCoinUICoroutine);
            _doubleCoinUICoroutine = null;
        }

        if (_jackpotFillBar != null)
        {
            _jackpotFillBar.fillAmount = 0f;
        }

        if (_doubleCoinFillBar != null)
        {
            _doubleCoinFillBar.fillAmount = 0f;
        }

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
