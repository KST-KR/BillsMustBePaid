using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : Singleton<DamageTextManager>
{
    #region 인스펙터
    [Header("설정")]
    [SerializeField] private DamageText _prefab;
    [SerializeField] protected Transform _canvasTransform;
    [SerializeField] protected int _initialPoolSize = 20;
    #endregion

    #region 내부 변수
    private readonly Queue<DamageText> _pool = new Queue<DamageText>();
    private readonly HashSet<DamageText> _activeTexts = new HashSet<DamageText>();
    private bool _poolInitialized;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }
        ClearPool();
    }
    #endregion

    #region 커스텀 함수(public)
    public void ShowDamage(int damage, Vector3 worldPos)
    {
        ShowDamage(damage, worldPos, false);
    }

    public void ShowDamage(int damage, Vector3 worldPos, bool isCritical)
    {
        if (!IsDamageTextEnabled() || _prefab == null)
        {
            return;
        }

        InitializePool();
        DamageText text = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_prefab, _canvasTransform);
        _activeTexts.Add(text);

        Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f), 0f);

        text.transform.position = worldPos + randomOffset;
        text.Init(damage, isCritical);
    }

    public void ReturnToPool(DamageText text)
    {
        if (text == null || !text.gameObject.activeSelf)
        {
            return;
        }

        if (!IsDamageTextEnabled())
        {
            text.gameObject.SetActive(false);
            Destroy(text.gameObject);
            return;
        }

        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }

    private void InitializePool()
    {
        if (_poolInitialized)
        {
            return;
        }

        _poolInitialized = true;
        if (_canvasTransform == null)
        {
            _canvasTransform = transform;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            DamageText item = Instantiate(_prefab, _canvasTransform);
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
    }

    private bool IsDamageTextEnabled()
    {
        return SettingsManager.Instance == null || SettingsManager.Instance.DamageTextEnabled;
    }

    private void HandleSettingsChanged()
    {
        if (!IsDamageTextEnabled())
        {
            ClearPool();
        }
    }

    private void ClearPool()
    {
        foreach (DamageText text in _pool)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
                Destroy(text.gameObject);
            }
        }
        _pool.Clear();

        foreach (DamageText text in _activeTexts)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
                Destroy(text.gameObject);
            }
        }
        _activeTexts.Clear();
        _poolInitialized = false;
    }
    #endregion

}
