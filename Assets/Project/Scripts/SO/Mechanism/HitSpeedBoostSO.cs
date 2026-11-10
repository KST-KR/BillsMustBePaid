using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName ="3D/Mechanism/Hit Speed Boost")]
public class HitSpeedBoostSO : MechanismSO
{
	#region 인스펙터
	[Header("부스트 설정")]
	[SerializeField] private float _boostMultiplier = 2.0f;
	[SerializeField] private float _duration = 3.0f;
    #endregion

    public override void Execute(GameObject target)
    {
        if(target.TryGetComponent(out PigBankMoveing move))
        {
            move.RunMechanismCoroutine(SpeedDecayRoutine(move));
        }
    }

    private IEnumerator SpeedDecayRoutine(PigBankMoveing move)
    {
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            move.SpeedMultiplier = Mathf.Lerp(_boostMultiplier, 1f, elapsed / _duration);
            yield return null;
        }

        move.SpeedMultiplier = 1f;
    }
}
