using UnityEngine;

/// <summary>
/// 플레이어가 장착한 망치 모델과 능력치를 관리
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    #region 인스펙터
    [Header("망치 장착 위치")]
    [SerializeField] private Transform _hammerMount;

    [Header("장비 관리")]
    [SerializeField] private EquipmentManager _equipmentManager;
    [SerializeField] private PlayerStats _playerStats;
    #endregion

    #region 내부 변수
    private EquipmentVisual[] _equipmentVisuals;
    private EquipmentData _currentEquipment;
    private EquipmentVisual _currentEquipmentVisual;
    #endregion

    #region 프로퍼티
    public EquipmentData CurrentEquipment => _currentEquipment;

    public Transform CurrentHammerTransform => _currentEquipmentVisual != null ? _currentEquipmentVisual.transform : null;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        if (_hammerMount == null)
        {
            Debug.LogError("HammerMount가 연결되지 않음.", this);
            return;
        }

        // 비활성화된 망치도 포함해서 가져오기
        _equipmentVisuals = _hammerMount.GetComponentsInChildren<EquipmentVisual>(true);
    }

    void Start()
    {
        if (_equipmentManager == null || _playerStats == null)
        {
            Debug.LogError("EquipmentManager 또는 PlayerStats가 연결되지 않음.", this);
            return;
        }

        // 현재 활성화된 망치를 기본 장비로 설정
        InitializeCurrentEquipment();
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
    #endregion

    #region 커스텀 (private)
    /// <summary>
    /// 현재 활성화된 망치를 기본 장비로 설정
    /// </summary>
    private void InitializeCurrentEquipment()
    {
        if (_equipmentVisuals == null)
        {
            return;
        }

        for (int i = 0; i < _equipmentVisuals.Length; i++)
        {
            EquipmentVisual visual = _equipmentVisuals[i];

            if (visual == null || !visual.gameObject.activeSelf)
            {
                continue;
            }

            if (visual.EquipmentData == null)
            {
                Debug.LogWarning($"{visual.name}: EquipmentData가 연결되지 않음.", visual);
                return;
            }

            _currentEquipment = visual.EquipmentData;
            _currentEquipmentVisual = visual;

            // 기본 장비를 보유 목록에 등록
            _equipmentManager.RegisterStartingEquipment(_currentEquipment);

            // 플레이어 능력치에 장비 능력치 반영
            _playerStats.SetHammerEquipment(_currentEquipment);

            return;
        }

        Debug.LogWarning("활성화된 망치 모델을 찾을 수 없음.", this);
    }

    /// <summary>
    /// 장비 구매 성공 이벤트 처리
    /// </summary>
    /// <param name="data"></param>
    private void HandleEquipmentEquipped(EquipmentData data)
    {
        if (data == null || _equipmentVisuals == null || _playerStats == null)
        {
            Debug.LogWarning("장착할 장비 또는 플레이어 참조가 올바르지 않음.", this);
            return;
        }

        // 이미 장착된 장비라면 중복 처리하지 않음
        if (_currentEquipment == data)
        {
            return;
        }

        // 요청한 장비와 일치하는 플레이어 망치 탐색
        EquipmentVisual targetVisual = null;

        for (int i = 0; i < _equipmentVisuals.Length; i++)
        {
            EquipmentVisual visual = _equipmentVisuals[i];

            if (visual != null && visual.EquipmentData == data)
            {
                targetVisual = visual;
                break;
            }
        }

        // 일치하는 모델이 없으면 기존 장착 상태 유지
        if (targetVisual == null)
        {
            Debug.LogWarning($"{data.EquipmentName}에 해당하는 플레이어 장착 모델을 찾을 수 없음.", this);
            return;
        }

        // 찾은 모델만 활성화하고 나머지는 비활성화
        for (int i = 0; i < _equipmentVisuals.Length; i++)
        {
            EquipmentVisual visual = _equipmentVisuals[i];

            if (visual != null)
            {
                visual.gameObject.SetActive(visual == targetVisual);
            }
        }

        _currentEquipment = data;
        _currentEquipmentVisual = targetVisual;

        // 플레이어 능력치에 장비 능력치 반영
        _playerStats.SetHammerEquipment(data);

        Debug.Log($"{data.EquipmentName} 장착 완료!");
    }
    #endregion
}
