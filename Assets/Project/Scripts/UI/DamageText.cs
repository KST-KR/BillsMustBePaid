using System.Collections;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    #region 인스펙터
    [Header("설정")]
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private float _duration = 0.8f;
    #endregion

    #region 내부 변수
    private Color _startColor;
    private Coroutine _animateCoroutine;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if(_text != null )
        {
            _startColor = _text.color;
        }
    }
    #endregion

    #region 커스텀 함수(public)
    public void Init(int damage)
    {
        if(_text != null)
        {
            _text.text = damage.ToString();
            _text.color = _startColor;
        }

        transform.localRotation = Quaternion.Euler(40f, 0f, 0f);

        gameObject.SetActive(true);

        if(_animateCoroutine != null)
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

        while(timer < _duration)
        {
            timer += Time.deltaTime;
            transform.position += Vector3.up * (_moveSpeed * Time.deltaTime);

            if (_text != null)
            {
                Color c = _startColor;
                c.a = Mathf.Lerp(1f, 0f, timer / _duration);
                _text.color = c;
            }
            yield return null;
        }

        _animateCoroutine = null;
        DamageTextManager.Instance.ReturnToPool(this);
    }
    #endregion
}
