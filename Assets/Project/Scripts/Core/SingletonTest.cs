using UnityEngine;

public class SingletonTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[1] SingletonTest Start 실행");
        Debug.Log($"[2] GameManager.Instance == null : {GameManager.Instance == null}");
        Debug.Log($"[3] GameManager.HasInstance : {GameManager.HasInstance}");
        Debug.Log($"[4] Time.timeScale : {Time.timeScale}");
    }
}