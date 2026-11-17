using TMPro;
using UnityEngine;

public class PigCrashSlotUI : MonoBehaviour
{
	#region 인스펙터
	[Header("UI가 담당할 돼지 번호 (0 ~ 9)")]
	[SerializeField] private int _pigIndex = 0;

	[Header("파괴 레벨 표시 텍스트")]
	[SerializeField] private TMP_Text _crashLevelText;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if(StageRewardManager.Instance != null)
        {
            StageRewardManager.Instance.OnPigCrashLevelChanged += HandleCrashLevelChanged;

            int currentLevel = StageRewardManager.Instance.GetPigCrashLevel(_pigIndex);
            UpdateText(currentLevel);
        }
    }

    private void OnDisable()
    {
        if (StageRewardManager.Instance != null)
        {
            StageRewardManager.Instance.OnPigCrashLevelChanged -= HandleCrashLevelChanged;
        }
    }
    #endregion

    #region 커스텀 함수(private)
    private void HandleCrashLevelChanged(int pigIndex, int newLevel)
    {
        if (pigIndex == _pigIndex)
        {
            UpdateText(newLevel);
        }
    }

    private void UpdateText(int level)
    {
        if (_crashLevelText != null)
        {
            _crashLevelText.text = $"Lv. {level}";
        }
    }
    #endregion
}
