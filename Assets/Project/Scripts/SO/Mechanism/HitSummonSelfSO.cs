using UnityEngine;

[CreateAssetMenu(menuName = "3D/Mechanism/Hit Summon Self")]
public class HitSummonSelfSO : MechanismSO
{
    [Header("Summon settings")]
    [SerializeField] private int _summonCount = 1;
    [SerializeField] private float _spawnRadius = 1.5f;
    [Tooltip("Summon chance (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float _summonChance = 1f;

    [Header("Clone pig data")]
    [SerializeField] private PigBankGameDataSO _cloneData;

    public override void Execute(GameObject target)
    {
        bool hasPrefab = _cloneData != null &&
            (_cloneData.PigPrefab != null ||
             (_cloneData.PigPrefabReference != null && _cloneData.PigPrefabReference.RuntimeKeyIsValid()));

        if (!hasPrefab)
        {
            Debug.LogWarning($"{name}: clone pig data or its prefab is missing.");
            return;
        }

        if (target == null || Random.value > _summonChance)
        {
            return;
        }

        for (int i = 0; i < _summonCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * _spawnRadius;
            Vector3 position = target.transform.position + new Vector3(offset.x, 0f, offset.y);

            if (PigBankObjectPoolManager.Instance == null)
            {
                continue;
            }

            GameObject clone = PigBankObjectPoolManager.Instance.SpawnPigBank(
                _cloneData.ID,
                position,
                target.transform.rotation);

            if (clone != null && SpawnPigBankManager.Instance != null)
            {
                SpawnPigBankManager.Instance.RegisterPigBank(clone);
            }
        }
    }
}