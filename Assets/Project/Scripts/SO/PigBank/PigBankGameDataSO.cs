using UnityEngine;

/// <summary>
/// [OBJECT - 01] 공통 부모 클래스 SO
/// </summary>
public abstract class PigBankGameDataSO : ScriptableObject
{
    #region 속성
    public string ID => _id;

    public string PigBankName => _pigBankName;
    public int PigBankHP => _pigBankHP;
    public float MoveSpeed => _moveSpeed;

    public GameObject PigPrefab => _pigPrefab;

    public int RewardCoin => _rewardCoin;

    public float JackpotChance => _jackpotChance;
    public float ShinePigBankChance => _shinePigBankChance;
    #endregion

    #region 인스펙터
    [Header("ID")]
    [SerializeField] protected string _id = "";

    [Header("돼지 저금통 정보")]
    [SerializeField] protected string _pigBankName = "";
    [SerializeField] protected int _pigBankHP;
    [SerializeField] protected float _moveSpeed;

    [Header("돼지 저금통 프리팹")]
    [SerializeField] protected GameObject _pigPrefab;

    [Header("파괴 레벨")]
    [SerializeField] protected int _crashLevel;

    [Header("코인 설정")]
    [SerializeField] protected int _rewardCoin;

    [Header("확률")]
    [Tooltip("잭팟 발생 확률 (0 = 0 %, 1 = 100 %)")]
    [SerializeField] protected float _jackpotChance = 0.05f;
    [Tooltip("빛나는 저금통 발생 확률 (0 = 0 %, 1 = 100 %)")]
    [SerializeField] protected float _shinePigBankChance = 0.01f;
    [Tooltip("특수 코인 등장 확률 (0 = 0 %, 1 = 100 %)")]
    [SerializeField] protected float _specialCoinChance = 0.02f;
    #endregion



    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_id))
        {
            Debug.LogWarning($"{name} : ID가 비어있다. 확인 필요");
        }
    }
}


