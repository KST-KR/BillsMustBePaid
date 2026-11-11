using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "3D/Mechanism/Die Black Hole")]
public class DieBlackHoleSO : MechanismSO
{
	#region 인스펙터
	[Header("블랙홀 설정")]
	[SerializeField] private float _pullRadius = 50f;
	[SerializeField] private float _pullDuration = 0.5f;
	[SerializeField] private float _gatherOffset = 1.5f;
	[SerializeField] private int _explosionDamage = 10;

	[Header("이펙트 설정")]
	[SerializeField] private GameObject _blackHoleEffectPrefab;
	[SerializeField] private float _effectDuration = 1.5f;
    #endregion

    public override void Execute(GameObject target)
    {
        Vector3 centerPos = target.transform.position;

        if (_blackHoleEffectPrefab != null)
        {
            GameObject effect = Instantiate(_blackHoleEffectPrefab, centerPos, Quaternion.identity);
            Destroy(effect, _effectDuration);
        }

        Collider[] colliders = Physics.OverlapSphere(centerPos, _pullRadius);

		foreach (Collider col in colliders)
		{
			if(col.gameObject == target)
			{
				continue;
			}
			
			if(col.TryGetComponent(out PigBankMoveing move) && col.TryGetComponent(out PigBankHealth health))
			{
				Vector2 randomOffset = Random.insideUnitCircle * _gatherOffset;
				Vector3 targetPos = new Vector3(centerPos.x + randomOffset.x, col.transform.position.y, centerPos.z + randomOffset.y);

				//대상 저금통 끌어당김, 데미지 코루틴 실행
				move.RunMechanismCoroutine(DamageRoutine(move, health, targetPos));
			}
		}
    }

	private IEnumerator DamageRoutine(PigBankMoveing move, PigBankHealth health, Vector3 targetPos)
	{
		Vector3 startPos = move.transform.position;
		float elapsed = 0f;

		move.IsStunned = true;

		while(elapsed < _pullDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / _pullDuration;

			Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);
            move.transform.position = new Vector3(nextPos.x, move.transform.position.y, nextPos.z);

            yield return null;
        }

        move.IsStunned = false;

        // 다 모였을 때 데미지 적용
        if (health != null && health.gameObject.activeInHierarchy)
        {
            health.TakePlayerAttack(_explosionDamage, false);
        }
    }
}
