
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로딩 화면의 UI 표시 및 갱신을 관리
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    #region 인스펙터
    [Header("로딩 UI")]
    [SerializeField] private Slider _loadingBar;
    [SerializeField] private TMP_Text _loadingText;
    #endregion

    private void Awake()
    {
        UpdateProgress(0f);
    }

    private void Update()
    {
        GameDataLoader dataLoader = GameDataLoader.Instance;
        if (dataLoader != null)
        {
            UpdateProgress(dataLoader.LoadProgress);
        }
    }

    #region UI 갱신
    /// <summary>
    /// 로딩 진행률과 문구 갱신
    /// </summary>
    public void UpdateProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (_loadingBar != null)
        {
            _loadingBar.value = progress;
        }

        if (_loadingText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100f);
            _loadingText.text = $"Loading... {percentage}%";
        }
    }
    #endregion
}
