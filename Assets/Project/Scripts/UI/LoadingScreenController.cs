using System.Collections;
using UnityEngine;

/// <summary>
/// 로딩 화면의 페이드 인/아웃 연출을 관리
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreenController : MonoBehaviour
{
    #region 인스펙터
    [Header("페이드 설정")]
    [SerializeField] private float _fadeDuration = 1f;
    #endregion

    #region 내부 변수
    private CanvasGroup _canvasGroup;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // 게임 최초 실행시 검은 화면으로 시작
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;
    }
    #endregion

    #region 페이드 연출
    /// <summary>
    /// 검은 화면에서 게임 화면으로 전환
    /// </summary>
    public IEnumerator FadeIn()
    {
        yield return Fade(1f, 0f);

        _canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 게임 화면에서 검은 화면으로 전환
    /// </summary>
    public IEnumerator FadeOut()
    {
        _canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f);
    }

    /// <summary>
    /// 지정한 시작 투명도에서 목표 투명도까지 변경
    /// </summary>
    /// <param name="startAlpha"></param>
    /// <param name="targetAlpha"></param>
    /// <returns></returns>
    private IEnumerator Fade(float startAlpha, float targetAlpha)
    {
        float elapsedTime = 0f;

        _canvasGroup.alpha = startAlpha;

        if (_fadeDuration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / _fadeDuration);

            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }
    #endregion

    /// <summary>
    /// 로딩 화면을 즉시 숨김
    /// </summary>
    public void HideImmediately()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }
}
