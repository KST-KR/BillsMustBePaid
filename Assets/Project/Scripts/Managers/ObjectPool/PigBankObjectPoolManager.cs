using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [OBJECT-02-01] 돼지 저금통 오브젝트 풀 매니저
/// </summary>
public class PigBankObjectPoolManager : MonoBehaviour
{
    #region 싱글톤
    public static PigBankObjectPoolManager Instance { get; private set; }
    #endregion

    #region 인스펙터
    [Header("등록된 돼지 저금통 리스트")]
    [SerializeField] private PigBankDataBaseSO _pigDatabase;

    [Header("기본 생성 개수")]
    [SerializeField] private int _generationCount = 30;
    #endregion

    #region 내부 변수
    private Dictionary<PigBankGameDataSO, Queue<GameObject>> _poolDistionary = new Dictionary<PigBankGameDataSO, Queue<GameObject>>();
    private Dictionary<PigBankGameDataSO, Transform> _rootDistionary = new Dictionary<PigBankGameDataSO, Transform>();
    #endregion

    #region 이벤트
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);

            return;
        }

        InitPools();
    }

    #endregion

    #region 커스텀 메소드 (Public)
    public GameObject SpawnPigBank(string pigID, Vector3 position, Quaternion rotation)
    {
        if(!_pigDatabase.TryGet(pigID, out PigBankGameDataSO pigData))
        {
            Debug.LogError($"ID에 해당하는 돼지 데이터를 찾을 수 없습니다 : {pigID}");

            return null;
        }

        if(!_poolDistionary.ContainsKey(pigData))
        {
            Debug.LogError($"해당 돼지 저금통{pigID}의 풀이 생성되지 않았다.");

            return null;
        }

        Queue<GameObject> queue = _poolDistionary[pigData];
        GameObject pig = (queue.Count > 0) ? queue.Dequeue() : Instantiate(pigData.PigPrefab, _rootDistionary[pigData]);

        pig.transform.position = position;
        pig.transform.rotation = rotation;
        pig.transform.SetParent(null);
        pig.SetActive(true);

        return pig;
    }

    public void ReturnPigBank(PigBankGameDataSO pigData, GameObject pig)
    {
        if(!_poolDistionary.ContainsKey(pigData))
        {
            return;
        }

        ResetPigBankObject(pig);

        pig.SetActive(false);
        pig.transform.SetParent(_rootDistionary[pigData]);
        _poolDistionary[pigData].Enqueue(pig);
    }
    #endregion

    #region 커스텀 메소드 (Private)
    private void InitPools()
    {
        if(_pigDatabase == null)
        {
            Debug.LogWarning("PigBankDataBaseSO가 연결되지 않았다. 확인필요");

            return;
        }

        foreach(var pigData in _pigDatabase.Datas)
        {
            if(pigData == null || pigData.PigPrefab == null) 
            {
                Debug.LogError("PigData가 없거나 PigPrefab이 존재하지 않음");
                continue;
            }

            GameObject rootObj = new GameObject($"PoolRoot_{pigData.ID}");
            rootObj.transform.SetParent(transform);
            _rootDistionary[pigData] = rootObj.transform;

            Queue<GameObject> queue = new Queue<GameObject>();

            for(int i = 0; i < _generationCount; i++)
            {
                GameObject pig = Instantiate(pigData.PigPrefab, rootObj.transform);
                pig.SetActive(false);
                queue.Enqueue(pig);
            }

            _poolDistionary[pigData] = queue;
        }
    }

    private void ResetPigBankObject(GameObject pig)
    {
        Rigidbody rb = pig.GetComponent<Rigidbody>();

        if(rb != null )
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
    #endregion

}
