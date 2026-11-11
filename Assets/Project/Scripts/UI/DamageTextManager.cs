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
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            DamageText item = Instantiate(_prefab, _canvasTransform);
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
    }
    #endregion

    #region 커스텀 함수(public)
    public void ShowDamage(int damage, Vector3 worldPos)
    {
        ShowDamage(damage, worldPos, false);
    }

    public void ShowDamage(int damage, Vector3 worldPos, bool isCritical)
    {
        DamageText text = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_prefab, _canvasTransform);

        Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f), 0f);

        text.transform.position = worldPos + randomOffset;
        text.Init(damage, isCritical);
    }

    public void ReturnToPool(DamageText text)
    {
        if(!text.gameObject.activeSelf)
        {
            return;
        }

        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }
    #endregion

}
