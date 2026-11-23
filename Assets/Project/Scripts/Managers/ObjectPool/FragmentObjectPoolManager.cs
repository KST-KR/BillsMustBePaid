using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FragmentObjectPoolManager : Singleton<FragmentObjectPoolManager>
{
    public enum EFragmentType { A, B, C, D, E }

    [Header("Addressable fragment 프리팹")]
    [SerializeField] private AssetReferenceGameObject _fragmentPrefabAReference;
    [SerializeField] private AssetReferenceGameObject _fragmentPrefabBReference;
    [SerializeField] private AssetReferenceGameObject _fragmentPrefabCReference;
    [SerializeField] private AssetReferenceGameObject _fragmentPrefabDReference;
    [SerializeField] private AssetReferenceGameObject _fragmentPrefabEReference;

    [Header("레거시 프리팹 폴백(Addressable 참조가 할당되지 않았을 때 사용됨)")]
    [SerializeField] private GameObject _fragmentPrefabA;
    [SerializeField] private GameObject _fragmentPrefabB;
    [SerializeField] private GameObject _fragmentPrefabC;
    [SerializeField] private GameObject _fragmentPrefabD;
    [SerializeField] private GameObject _fragmentPrefabE;

    [Header("풀 설정")]
    [SerializeField] private int _fragmentCount = 20;
    [SerializeField] private float _minDistance = 2f;
    [SerializeField] private float _maxDistance = 4f;
    [SerializeField] private float _upModifier = 1f;
    [SerializeField] private float _torqueMin = 1f;
    [SerializeField] private float _torqueMax = 3f;
    [SerializeField] private float _removeTime = 2f;

    private readonly Dictionary<EFragmentType, Queue<GameObject>> _poolDictionary = new Dictionary<EFragmentType, Queue<GameObject>>();
    private readonly Dictionary<EFragmentType, GameObject> _prefabDictionary = new Dictionary<EFragmentType, GameObject>();
    private readonly List<AsyncOperationHandle<GameObject>> _loadedHandles = new List<AsyncOperationHandle<GameObject>>();
    private Transform _poolRoot;
    private bool _isPreparingPools;
    private bool _poolsReady;

    public bool IsEnabled => SettingsManager.Instance == null || SettingsManager.Instance.FragmentAmount > 0;

    public GameObject SpawnFragment(EFragmentType type, Vector3 spawnPosition)
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.FragmentAmount == 0)
            return null;

        if (!_poolsReady || !_poolDictionary.TryGetValue(type, out Queue<GameObject> queue) || !_prefabDictionary.TryGetValue(type, out GameObject prefab))
            return null;

        GameObject fragment = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab, _poolRoot);
        fragment.transform.SetParent(null);
        fragment.transform.SetPositionAndRotation(spawnPosition, Random.rotation);
        fragment.SetActive(true);
        ApplyExplosionPhysics(fragment);
        StartCoroutine(AfterTimeCoReturn(fragment, type, _removeTime));
        return fragment;
    }

    public void SpawnRandomFragments(Vector3 spawnPosition, int spawnCount = -1, float spawnInterval = 0.03f)
    {
        int fragmentOption = SettingsManager.Instance != null ? SettingsManager.Instance.FragmentAmount : 3;
        if (fragmentOption == 0)
            return;

        if (spawnCount < 0)
        {
            switch (fragmentOption)
            {
                case 1: spawnCount = 1; break;
                case 2: spawnCount = 3; break;
                default: spawnCount = 5; break;
            }
        }

        StartCoroutine(PrepareAndSpawnFragments(spawnPosition, spawnCount, spawnInterval));
    }

    protected override void OnSingletonDestroy()
    {
        for (int i = 0; i < _loadedHandles.Count; i++)
        {
            if (_loadedHandles[i].IsValid())
                Addressables.Release(_loadedHandles[i]);
        }
        _loadedHandles.Clear();
    }

    private IEnumerator PrepareAndSpawnFragments(Vector3 spawnPosition, int spawnCount, float spawnInterval)
    {
        while (_isPreparingPools)
            yield return null;

        if (!_poolsReady)
            yield return PreparePoolsAsync();

        if (!_poolsReady)
        {
            Debug.LogError("단편 풀을 준비할 수 없음.", this);
            yield break;
        }

        yield return RandomFragmentsCoroutine(spawnPosition, spawnCount, spawnInterval);
    }

    public IEnumerator PreparePoolsAsync(System.Action<float> onProgress = null)
    {
        if (!IsEnabled)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        while (_isPreparingPools)
            yield return null;

        if (_poolsReady)
        {
            onProgress?.Invoke(1f);
            yield break;
        }
        _isPreparingPools = true;
        if (_poolRoot == null)
        {
            GameObject root = new GameObject("FragmentPool_Root");
            root.transform.SetParent(transform);
            _poolRoot = root.transform;
        }

        EFragmentType[] types = { EFragmentType.A, EFragmentType.B, EFragmentType.C, EFragmentType.D, EFragmentType.E };
        for (int i = 0; i < types.Length; i++)
        {
            EFragmentType type = types[i];
            AssetReferenceGameObject reference = GetReferenceByType(type);
            GameObject prefab = null;

            if (reference != null && reference.RuntimeKeyIsValid())
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(reference);
                while (!handle.IsDone)
                {
                    float typeProgress = handle.PercentComplete * 0.8f;
                    onProgress?.Invoke((i + typeProgress) / types.Length);
                    yield return null;
                }
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    prefab = handle.Result;
                    _loadedHandles.Add(handle);
                }
                else
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                    Debug.LogError($"Addressable 프래그먼트를 로드하지 못함: {type}", this);
                }
            }

            if (prefab == null)
                prefab = GetLegacyPrefabByType(type);

            if (prefab == null)
            {
                Debug.LogWarning($"해당 유형에 할당된 프래그먼트 프리팹이 없음: {type}.", this);
                continue;
            }

            _prefabDictionary[type] = prefab;
            Queue<GameObject> queue = new Queue<GameObject>();
            for (int j = 0; j < Mathf.Max(0, _fragmentCount); j++)
            {
                GameObject fragment = Instantiate(prefab, _poolRoot);
                fragment.SetActive(false);
                queue.Enqueue(fragment);
            }
            _poolDictionary[type] = queue;
            onProgress?.Invoke((float)(i + 1) / types.Length);
        }

        _poolsReady = true;
        _isPreparingPools = false;
    }

    private AssetReferenceGameObject GetReferenceByType(EFragmentType type)
    {
        switch (type)
        {
            case EFragmentType.A: return _fragmentPrefabAReference;
            case EFragmentType.B: return _fragmentPrefabBReference;
            case EFragmentType.C: return _fragmentPrefabCReference;
            case EFragmentType.D: return _fragmentPrefabDReference;
            case EFragmentType.E: return _fragmentPrefabEReference;
            default: return null;
        }
    }

    private GameObject GetLegacyPrefabByType(EFragmentType type)
    {
        switch (type)
        {
            case EFragmentType.A: return _fragmentPrefabA;
            case EFragmentType.B: return _fragmentPrefabB;
            case EFragmentType.C: return _fragmentPrefabC;
            case EFragmentType.D: return _fragmentPrefabD;
            case EFragmentType.E: return _fragmentPrefabE;
            default: return null;
        }
    }

    private void ApplyExplosionPhysics(GameObject fragment)
    {
        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = Mathf.Abs(randomDir.y) + _upModifier;
        randomDir.Normalize();
        rb.AddForce(randomDir * Random.Range(_minDistance, _maxDistance), ForceMode.Impulse);
        rb.AddTorque(new Vector3(
            Random.Range(_torqueMin, _torqueMax),
            Random.Range(_torqueMin, _torqueMax),
            Random.Range(_torqueMin, _torqueMax)), ForceMode.Impulse);
    }

    private IEnumerator AfterTimeCoReturn(GameObject fragment, EFragmentType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (fragment != null && fragment.activeSelf)
            ReturnToPool(fragment, type);
    }

    private IEnumerator RandomFragmentsCoroutine(Vector3 spawnPosition, int spawnCount, float spawnInterval)
    {
        EFragmentType[] types = { EFragmentType.A, EFragmentType.B, EFragmentType.C, EFragmentType.D, EFragmentType.E };
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnFragment(types[Random.Range(0, types.Length)], spawnPosition);
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

        if (_poolDictionary.TryGetValue(type, out Queue<GameObject> queue))
            queue.Enqueue(fragment);
    }
}
