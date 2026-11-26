using System.Collections;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 플레이어가 장착한 망치 모델과 능력치를 관리
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [Header("망치 장착 위치")]
    [SerializeField] private Transform _hammerMount;

    [Header("기본 장비")]
    [SerializeField] private EquipmentData _startingEquipment;

    [Header("장비 관리")]
    [SerializeField] private EquipmentManager _equipmentManager;
    [SerializeField] private PlayerStats _playerStats;

    private EquipmentData _currentEquipment;
    private Transform _currentHammerTransform;
    private AsyncOperationHandle<GameObject> _currentHammerHandle;
    private bool _hasCurrentHammerHandle;
    private int _equipRequestVersion;
    private EquipmentData _pendingEquipment;

    public event Action<EquipmentData> OnEquipmentChanged;

    public EquipmentData CurrentEquipment => _currentEquipment;
    public Transform CurrentHammerTransform => _currentHammerTransform;
    public EquipmentData StartingEquipment => _startingEquipment;

    public IEnumerator PrepareInitialHammerAsync(EquipmentData equipment, Action<float> onProgress = null)
    {
        if (_hammerMount == null || _equipmentManager == null || _playerStats == null)
        {
            Debug.LogError("PlayerEquipment의 HammerMount, EquipmentManager 또는 PlayerStats가 연결되지 않음.", this);
            onProgress?.Invoke(1f);

            yield break;
        }

        if (_startingEquipment == null || equipment == null)
        {
            Debug.LogError("PlayerEquipment의 기본 장비 또는 초기 장비가 연결되지 않음.", this);
            onProgress?.Invoke(1f);

            yield break;
        }

        _equipmentManager.RegisterStartingEquipment(_startingEquipment);

        yield return EquipHammerAsync(equipment, false, onProgress);
    }

    private void OnEnable()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentEquipped += HandleEquipmentEquipped;
        }
    }

    private void OnDisable()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentEquipped -= HandleEquipmentEquipped;
        }
    }

    private void OnDestroy()
    {
        _equipRequestVersion++;
        ReleaseCurrentHammer();
    }

    private void HandleEquipmentEquipped(EquipmentData data)
    {
        if (data == null || _hammerMount == null || _playerStats == null)
        {
            Debug.LogWarning("장착할 장비 또는 PlayerEquipment 참조가 올바르지 않음.", this);
            return;
        }

        if (_currentEquipment == data && _currentHammerTransform != null)
        {
            return;
        }

        // 구매 직후 Addressables 로딩 중 같은 아이템을 다시 클릭해도
        // 진행 중인 첫 장착 요청을 취소하지 않음.
        if (_pendingEquipment == data)
        {
            return;
        }

        StartCoroutine(EquipHammerAsync(data, _currentEquipment != null && _currentEquipment != data));
    }

    private IEnumerator EquipHammerAsync(EquipmentData data, bool playChangeSound, Action<float> onProgress = null)
    {
        if (data.HammerPrefabReference == null || !data.HammerPrefabReference.RuntimeKeyIsValid())
        {
            if (_pendingEquipment == data)
            {
                _pendingEquipment = null;
            }

            Debug.LogError($"{data.EquipmentName}: Addressable 해머 프리팹 참조가 설정되지 않음.", this);
            onProgress?.Invoke(1f);

            yield break;
        }

        int requestVersion = ++_equipRequestVersion;
        _pendingEquipment = data;
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(data.HammerPrefabReference, _hammerMount, false);

        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete);

            yield return null;
        }

        onProgress?.Invoke(1f);

        if (requestVersion != _equipRequestVersion)
        {
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                Addressables.ReleaseInstance(handle);
            }
            else if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            
            yield break;
        }

        _pendingEquipment = null;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            Debug.LogError($"Addressable 해머 프리팹을 생성하지 못함: {data.EquipmentName}", this);

            yield break;
        }

        ReleaseCurrentHammer();
        _currentHammerHandle = handle;
        _hasCurrentHammerHandle = true;
        _currentHammerTransform = handle.Result.transform;
        PrepareHammerForAttachment(handle.Result);
        SetLayerRecursively(handle.Result.transform, _hammerMount.gameObject.layer);
        _currentEquipment = data;
        _playerStats.SetHammerEquipment(data);
        OnEquipmentChanged?.Invoke(data);

        if (playChangeSound)
        {
            AudioManager.Instance?.PlaySFX(EAudioType.ChangeHammer);
        }
    }

    private void ReleaseCurrentHammer()
    {
        _currentHammerTransform = null;

        if (!_hasCurrentHammerHandle)
        {
            return;
        }

        if (_currentHammerHandle.IsValid())
        {
            Addressables.ReleaseInstance(_currentHammerHandle);
        }

        _hasCurrentHammerHandle = false;
    }

    private static void PrepareHammerForAttachment(GameObject hammer)
    {
        // Addressable 프리팹 중 일부는 풀 용도로 루트가 비활성화된 상태로 저장되어 있음.
        // 플레이어 손에 장착할 때는 루트를 활성화해야 렌더러도 표시됨.
        hammer.SetActive(true);

        Rigidbody body = hammer.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = hammer.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        Collider[] colliders = hammer.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
