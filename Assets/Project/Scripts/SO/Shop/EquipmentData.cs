using UnityEngine;

/// <summary>
/// 상점에서 판매하는 장비의 기본 데이터를 관리
/// </summary>
[CreateAssetMenu(fileName = "EquipmentData", menuName = "Shop/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    #region 인스펙터
    [Header("장비 정보")]
    [SerializeField] private string _equipmentName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private int _price;

    [Header("장비 능력치")]
    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackInterval;
    [SerializeField] private float _attackRange;

    [Header("장착 모델")]
    [SerializeField] private GameObject _equippedPrefab;
    #endregion

    #region 프로퍼티
    public string EquipmentName => _equipmentName;
    public string Description => _description;
    public int Price => _price;

    public float AttackPower => _attackPower;
    public float AttackInterval => _attackInterval;
    public float AttackRange => _attackRange;

    public GameObject EquippedPrefab => _equippedPrefab;
    #endregion

}
