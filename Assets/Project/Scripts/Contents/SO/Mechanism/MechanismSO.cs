using UnityEngine;

public abstract class MechanismSO : ScriptableObject
{
    public abstract void Execute(GameObject target);
}
