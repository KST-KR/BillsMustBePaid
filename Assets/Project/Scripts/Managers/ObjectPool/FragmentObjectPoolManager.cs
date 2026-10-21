using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FragmentObjectPoolManager;

public class FragmentObjectPoolManager : MonoBehaviour
{
    #region Enum
    public enum EFragmentType
    {
        A,
        B,
        C, 
        D,
        E
    }
    #endregion

    #region 싱글톤
    public static FragmentObjectPoolManager Instance { get; private set; }
    #endregion

    #region 인스펙터
    [Header("파편 설정")]
    [SerializeField] private GameObject _fragmentPrefabA;
    [SerializeField] private GameObject _fragmentPrefabB;
    [SerializeField] private GameObject _fragmentPrefabC;
    [SerializeField] private GameObject _fragmentPrefabD;
    [SerializeField] private GameObject _fragmentPrefabE;

    [Header("생성 개수")]
    [SerializeField] private int _fragmentCount = 20;

    [Header("퍼짐 및 이동 거리")]
    [Tooltip("최소 날아가는 거리")]
    [SerializeField] private float _minDistance = 2.0f;
    [Tooltip("최대 날아가는 거리")]
    [SerializeField] private float _maxDistance = 4.0f;
    [Tooltip("위로 솟구치는 정도")]
    [SerializeField] private float _upModifier = 1.0f;
    [Tooltip("회전력 최소")]
    [SerializeField] private float _torqueMin = 1.0f;
    [Tooltip("회전력 최대")]
    [SerializeField] private float _torqueMax = 3.0f;

    [Header("수명 설정")]
    [SerializeField] private float _removeTime = 2.0f;
    #endregion

    #region 내부 변수
    private Dictionary<EFragmentType, Queue<GameObject>> _poolDictionary;
    private Transform _poolRoot;
    #endregion

    #region 이벤트
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);

            return;
        }

        InitializePools();
    }
    #endregion

    #region 커스텀 메소드 (Public)
    public GameObject SpawnFragment(EFragmentType type, Vector3 spawnPosition)
    {
        if(!_poolDictionary.ContainsKey(type))
        {
            Debug.LogWarning($"존재하지 않는 파편 입니다{type}");

            return null;
        }

        Queue<GameObject> queue = _poolDictionary[type];
        GameObject prefabRef = GetPrefabByType(type);

        GameObject fragment = (queue.Count > 0) ? queue.Dequeue() : Instantiate(prefabRef);

        fragment.transform.position = spawnPosition;
        fragment.transform.rotation = Random.rotation;
        fragment.transform.SetParent(null);
        fragment.SetActive(true);

        ApplyExplosionPhysics(fragment);

        StartCoroutine(AfterTimeCoReturn(fragment, type, _removeTime));

        return fragment;
    }
    
    public void SpawnRandomFragments(Vector3 spawnPosition, int spawnCount = 5, float spawnInterval = 0.03f)
    {
        StartCoroutine(RandomFragmentsCoroutine(spawnPosition, spawnCount, spawnInterval));
    }
    #endregion

    #region 커스텀 메소드 (Private)
    /// <summary>
    ///  초기화
    /// </summary>
    private void InitializePools()
    {
        GameObject rootObj = new GameObject("FragmentPool_Root");
        _poolRoot = rootObj.transform;

        _poolDictionary = new Dictionary<EFragmentType, Queue<GameObject>>
        {
            { EFragmentType.A, QueueForPrefab(_fragmentPrefabA) },
            { EFragmentType.B, QueueForPrefab(_fragmentPrefabB) },
            { EFragmentType.C, QueueForPrefab(_fragmentPrefabC) },
            { EFragmentType.D, QueueForPrefab(_fragmentPrefabD) },
            { EFragmentType.E, QueueForPrefab(_fragmentPrefabE) }
        };
    }

    private Queue<GameObject> QueueForPrefab(GameObject prefab)
    {
        Queue<GameObject> queue = new Queue<GameObject>();
        if (prefab == null) return queue;

        for (int i = 0; i < _fragmentCount; i++)
        {
            GameObject obj = Instantiate(prefab, _poolRoot);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
        return queue;
    }

    private GameObject GetPrefabByType(EFragmentType type)
    {
        switch (type)
        {
            case EFragmentType.A: 
                return _fragmentPrefabA;

            case EFragmentType.B: 
                return _fragmentPrefabB;

            case EFragmentType.C: 
                return _fragmentPrefabC;

            case EFragmentType.D: 
                return _fragmentPrefabD;

            case EFragmentType.E: 
                return _fragmentPrefabE;

            default: 
                return null;
        }
    }

    private void ApplyExplosionPhysics(GameObject fragment)
    {
        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();

            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Abs(randomDir.y) + _upModifier; 
            randomDir.Normalize();

            float distanceForce = Random.Range(_minDistance, _maxDistance);
            rb.AddForce(randomDir * distanceForce, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(_torqueMin, _torqueMax),
                Random.Range(_torqueMin, _torqueMax),
                Random.Range(_torqueMin, _torqueMax)
            );
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }
    }

    private IEnumerator AfterTimeCoReturn(GameObject fragment, EFragmentType type, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fragment != null && fragment.activeSelf)
        {
            ReturnToPool(fragment, type);
        }
    }

    /// <summary>
    /// 파편을 랜덤으로 흩뿌리는 코루틴
    /// </summary>
    private IEnumerator RandomFragmentsCoroutine(Vector3 spawnPosition, int spawnCount, float spawnInterval)
    {
        EFragmentType[] allTypes = new EFragmentType[]
        {
            EFragmentType.A,
            EFragmentType.B,
            EFragmentType.C,
            EFragmentType.D,
            EFragmentType.E
        };

        for (int i = 0; i < spawnCount; i++)
        {
            EFragmentType randomType = allTypes[Random.Range(0, allTypes.Length)];

            SpawnFragment(randomType, spawnPosition);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void ReturnToPool(GameObject fragment, EFragmentType type)
    {
        fragment.SetActive(false);
        fragment.transform.SetParent(_poolRoot);

        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (_poolDictionary.ContainsKey(type))
        {
            _poolDictionary[type].Enqueue(fragment);
        }
    }
    #endregion
}
