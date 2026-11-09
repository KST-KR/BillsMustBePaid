using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    #region 싱글톤
    public static DamageTextManager Instance { get; private set; }
    #endregion

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
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

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
        DamageText text = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_prefab, _canvasTransform);

        Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f), 0f);
        text.transform.position = worldPos + randomOffset;
        text.Init(damage);
    }

    public void ReturnToPool(DamageText text)
    {
        if(!text.gameObject.activeSelf)
        {
            return;
        }

        text.gameObject.SetActive(true);
        _pool.Enqueue(text);
    }
    #endregion

}
