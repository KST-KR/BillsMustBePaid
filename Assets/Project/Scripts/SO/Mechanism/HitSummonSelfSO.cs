using UnityEngine;

[CreateAssetMenu(menuName = "3D/Mechanism/Hit Summon Self")]
public class HitSummonSelfSO : MechanismSO
{
    #region 인스펙터
    [Header("소환 설정")]
    [SerializeField] private int _summonCount = 1;
    [SerializeField] private float _spawnRadius = 1.5f;
    [Tooltip("소환 확률 (0 = 0 %, 1 = 100 %")]
    [Range(0f, 1f)]
    [SerializeField] private float _summonChance = 1.0f;

    [Header("분신 데이터")]
    [SerializeField] private PigBankGameDataSO _cloneDate;
    #endregion

    public override void Execute(GameObject target)
    {
        if(_cloneDate == null || _cloneDate.PigPrefab == null)
        {
            Debug.LogWarning($"{name} : 분신 데이터(_cloneData) 또는 프리팹이 없습니다. 확인 필요");
            return;
        }

        if(Random.value > _summonChance)
        {
            return;
        }

        for(int i = 0; i < _summonCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * _spawnRadius;
            Vector3 spawnPos = target.transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

           if(PigBankObjectPoolManager.Instance != null )
            {
                PigBankObjectPoolManager.Instance.SpawnPigBank(_cloneDate.ID, spawnPos, target.transform.rotation);
            }
        }
    }
}
