using System.Collections;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    #region 인스펙터
    [Header("PigBank Color Data")]
    [SerializeField] private PigBankColorData _pigData;
    #endregion

    #region 내부 변수
    private Renderer _renderer;
    private Material[] _materials;
    private Color[] _originColors;

    private Color _hitColor = Color.red;
    private float _changeTime = 0.23f;
    private Coroutine _coroutine;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer != null)
        {
            _materials = _renderer.materials;

            _originColors = new Color[_materials.Length];
            for (int i = 0; i < _materials.Length; i++)
            {
                _originColors[i] = _materials[i].color;
            }

            Debug.Log($"[HitEffect] 머티리얼 개수: {_materials.Length}");
        }
    }
    #endregion

    #region 커스텀 함수(public)
    public void ChangeColor()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(CoChangeColor());
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator CoChangeColor()
    {
        if (_materials != null)
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                Debug.Log("피격! 색 바꿈");
                _materials[i].color = _hitColor;
            }
        }

        // Update 문 없이 while과 Time.deltaTime을 이용해 시간 측정
        float elapsedTime = 0f;
        while (elapsedTime < _changeTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        // 시간이 다 지나면 원래 색상으로 복구
        if (_materials != null)
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                Debug.Log("색 복구");
                _materials[i].color = _originColors[i];
            }
        }

        _coroutine = null;
    }
    #endregion
}
