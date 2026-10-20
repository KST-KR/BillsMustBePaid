using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DataBaseSO<T> : ScriptableObject, ISerializationCallbackReceiver where T : PigBankGameDataSO
{
    #region 속성
    public IReadOnlyList<T> Datas => _datas;
    public int Count => _datas.Count;
    #endregion

    #region 인덱서
    public T this[int index] => _datas[index];
    #endregion

    #region 인스펙터
    [SerializeField] private List<T> _datas = new List<T>();
    #endregion

    #region 내부 변수
    [NonSerialized] private Dictionary<string, T> _lookup;
    [NonSerialized] private bool _isDirty = true;
    #endregion

    #region 이벤트
    protected virtual void OnValidate()
    {
        HashSet<string> ids = new HashSet<string>();

        for(int i = 0; i < _datas.Count; i++)
        {
            T data = _datas[i];

            if(data == null)
            {
                continue;
            }

            string id = data.ID;

            if(string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"{name} / {i} → Data의 ID가 비어있디.");

                continue;
            }

            if(!ids.Add(id))
            {
                Debug.LogWarning($"{name} : 중복 ID발견 {id}");
            }

            _isDirty = true;
        }
    }
    #endregion

    #region 커스텀 메소드 (Public)
    public bool TryGet(string id, out T data)
    {
        EnsureLookup();

        return _lookup.TryGetValue(id, out data);
    }

    public void OnBeforeSerialize()
    {
        // Do Nothing
    }

    public void OnAfterDeserialize()
    {
        _isDirty = true;
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void EnsureLookup()
    {
        if(!_isDirty && _lookup != null)
        {
            return;
        }

        _lookup ??= new Dictionary<string, T>();

        _lookup.Clear();

        for(int i = 0; i < _datas.Count; i++)
        {
            T data = _datas[i];

            if (data == null || string.IsNullOrWhiteSpace(data.ID))
            {
                continue;
            }

            _lookup.TryAdd(data.ID, data);
        }

        _isDirty = false;
    }
    #endregion
}
