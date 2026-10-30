using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    #region 인스펙터
    [Header("PigBank Color Data")]
    [SerializeField] private PigBankColorData _pigData;
    #endregion

    #region 내부 변수
    private Material[] _materials;
    private Color[] _originColors;

    private Color _hitColor = Color.red;
    private float _changeTime = 0.23f;
    private Coroutine _coroutine;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        Renderer[] renderer = GetComponentsInChildren<Renderer>();
        List<Material> materialsList = new List<Material>();

        foreach(var rend in renderer)
        {
            materialsList.Add(rend.material);
        }
    
        _materials = materialsList.ToArray();

        _originColors = new Color[_materials.Length];
        for (int i = 0; i < _materials.Length; i++)
        {
            _originColors[i] = _materials[i].color;
        }

        Debug.Log($"[HitEffect] 머티리얼 개수: {_materials.Length}");
    }

    private void OnEnable()
    {
        _coroutine = null;
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

    public void RestoreColor()
    {
        // 진행중이던 코루틴이 있다면 강제로 중지
        if ( _coroutine != null )
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        if (_materials != null)
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i].color = _originColors[i];
            }
        }
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator CoChangeColor()
    {
        if (_materials != null)
        {
            Debug.Log("피격! 색 바꿈");
            for (int i = 0; i < _materials.Length; i++)
            {
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

        RestoreColor();

        _coroutine = null;
    }
    #endregion
}
