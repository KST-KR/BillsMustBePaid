using System.Collections;
using TMPro;
using UnityEngine;

public class RewardText : MonoBehaviour
{
    #region 인스펙터
    [Header("설정")]
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private float _duration = 0.8f;

    [Header("잭팟  설정")]
    // 잭팟 컬러 황금색 설정
    [SerializeField] private Color _jackpotColor = Color.yellow;
    [SerializeField, Min(1f)] private float _jackpotScaleMultiplier = 1.5f;
    #endregion

    #region 내부 변수
    private Color _startColor;
    private Vector3 _originalScale;
    private Coroutine _animateCoroutine;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (_text != null)
        {
            _startColor = _text.color;
        }

        _originalScale = transform.localScale;
    }
    #endregion

    #region 커스텀 함수(public)
    public void Init(int reward, bool isJackpot = false)
    {
        if (_text != null)
        {
            _text.text = isJackpot ? $"+${reward.ToString()}" : $"+${reward.ToString()}";

            // 이전 공격의 색상 설정이 남지 않도록 초기화
            _text.color = isJackpot ? _jackpotColor : _startColor;

            _text.enabled = false;
        }

        // 일반 공격은 원래 크기, 크리티컬은 1.5배
        transform.localScale = isJackpot ? _originalScale * _jackpotScaleMultiplier : _originalScale;

        transform.localRotation = Quaternion.Euler(40f, 0f, 0f);

        gameObject.SetActive(true);

        if (_animateCoroutine != null)
        {
            StopCoroutine(_animateCoroutine);
        }
        _animateCoroutine = StartCoroutine(AnimateRoutione());
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator AnimateRoutione()
    {
        float timer = 0f;

        // 텍스트가 나타나기 전에 잠시 대기 (데미지 먼저 표시를 위해)
        yield return new WaitForSeconds(0.5f);

        if (_text != null)
        {
            _text.enabled = true;
        }

        while (timer < _duration)
        {
            timer += Time.deltaTime;
            transform.position += Vector3.up * (_moveSpeed * Time.deltaTime);

            if (_text != null)
            {
                Color color = _text.color;
                color.a = Mathf.Lerp(1f, 0f, timer / _duration);
                _text.color = color;
            }
            yield return null;
        }

        _animateCoroutine = null;
        RewardTextManager.Instance.ReturnToPool(this);
    }
    #endregion
}
