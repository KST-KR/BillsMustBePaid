using UnityEngine;

public class PlayerStaminaDebugTester : MonoBehaviour
{
    #region 내부 변수
    private PlayerStamina _playerStamina;
    #endregion

    #region 이벤트
    private void Awake()
    {
        _playerStamina = GetComponent<PlayerStamina>();
    }

    private void OnEnable()
    {
        if (_playerStamina == null)
        {
            return;
        }

        _playerStamina.OnStaminaChanged += HandleStaminaChanged;
        _playerStamina.OnStaminaDepleted += HandleStaminaDepleted;
    }

    private void OnDisable()
    {
        if (_playerStamina == null)
        {
            return;
        }

        _playerStamina.OnStaminaChanged -= HandleStaminaChanged;
        _playerStamina.OnStaminaDepleted -= HandleStaminaDepleted;
    }
    #endregion

    #region 테스트 입력
    private void Update()
    {
        if (_playerStamina == null)
        {
            return;
        }

        // C 키: 스태미나 30 소모
        if (Input.GetKeyDown(KeyCode.C))
        {
            float consumedAmount = _playerStamina.ConsumeStamina(30f);

            Debug.Log($"실제 소모량: {consumedAmount}");
        }

        // R 키: 스태미나 20 회복
        if (Input.GetKeyDown(KeyCode.R))
        {
            float recoveredAmount = _playerStamina.RecoverStamina(20f);

            Debug.Log($"실제 회복량: {recoveredAmount}");
        }
    }
    #endregion

    #region 이벤트 처리
    private void HandleStaminaChanged(float currentStamina, float maxStamina)
    {
        Debug.Log($"스태미나 변경: {currentStamina} / {maxStamina}");
    }

    private void HandleStaminaDepleted()
    {
        Debug.Log("스태미나 고갈: 사망 조건 이벤트 발생");
    }
    #endregion
}