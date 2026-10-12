using System;
using UnityEngine;

namespace ConventionSamples
{
    #region Enum

    /// <summary>
    /// 열거형 설명 E+PascalCase로 작성하며, 항목은 PascalCase로 작성합니다.
    /// </summary>
    public enum EGameState
    {
        /// <summary>
        /// 항목 설명
        /// </summary>
        Playing,
        Paused,
        GameOver
    }

    #endregion

    #region 인터페이스 예시

    /// <summary>
    /// 피해를 받을 수 있는 객체의 계약입니다. Interface는 클래스 위에 I + PascalCase로 정의합니다.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int damage);
    }

    #endregion

    /// <summary>
    /// 오락실 팀 코드 규칙 참고용 샘플 스크립트 입니다
    /// </summary>
    public class ScriptTemplate : SampleManagerBase
    {
        #region 상수

        // 반복 사용하거나 의미가 중요한 값은 상수/변수로 분리합니다.
        // 상수는 클래스 최상단에 C + PascalCase로 작성합니다.
        private const int CMaxBillCount = 10;
        private const float CMaxMoveSpeed = 10.0f;

        #endregion

        #region 싱글톤

        // Singleton은 여러 시스템이 접근하는 단일 Manager 중심으로 사용합니다.
        // 예: GameManager, AudioManager, MoneyManager, 필요 시 UIManager.
        // 그 외 시스템에 추가할 때는 팀원과 상의하며 모든 클래스에 무분별하게 적용하지 않습니다.
        public static ScriptTemplate Instance { get; private set; }

        #endregion

        #region 속성

        // 외부 읽기는 Property로 제공하고, 변경은 기능을 가진 Public Method로 제공합니다.
        // Singleton 내부 값도 Instance.Money처럼 Property를 통해 읽습니다.
        public int Money => _money;
        public int CurrentHp => _currentHp;
        public int PaidBillCount => _paidBillCount;
        public EGameState State => _state;

        #endregion

        #region 이벤트

        // 이벤트 이름은 발생 의미가 명확하도록 작성합니다.
        public event Action OnGameOver;
        public event Action<int> OnMoneyChanged;

        #endregion

        #region 인스펙터

        // Inspector 변수는 [SerializeField] private을 기본으로 하고 기능별 Header를 사용합니다.
        // 외부 접근이 실제 필요한 경우에만 public을 고려하며, Public Field 직접 노출은 지양합니다.
        [Header("이동 설정")]
        [SerializeField] private float _moveSpeed = 3.0f;
        [SerializeField] private float _rotateSpeed = 180.0f;

        [Header("공격 설정")]
        [SerializeField] private int _damage = 10;

        [Header("체력 설정")]
        [SerializeField] private int _startHp = 100;

        [Header("청구서 설정")]
        [SerializeField] private int _startMoney = 1000;
        [SerializeField] private int _billAmount = 100;

        #endregion

        #region 내부 변수

        // Inspector 변수와 내부 변수는 모두 _ + camelCase로 통일합니다.
        private int _money;
        private int _currentHp;
        private int _paidBillCount;
        private EGameState _state;
        private Vector3 _moveDirection;

        #endregion

        #region 이벤트

        // 작성 순서: Awake → OnEnable → Start → Update → FixedUpdate → LateUpdate → OnDisable → 기타.

        /// <summary>
        /// 단일 Manager를 등록하고 초기 상태를 준비합니다.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 활성화 시 게임 종료 이벤트를 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            OnGameOver += HandleGameOver;
        }

        /// <summary>
        /// 초기화된 돈을 구독자에게 알립니다.
        /// </summary>
        private void Start()
        {
            
        }

        /// <summary>
        /// 매 프레임 입력을 읽습니다.
        /// </summary>
        private void Update()
        {
           
        }

        /// <summary>
        /// 고정 주기로 이동을 처리합니다.
        /// </summary>
        private void FixedUpdate()
        {
           
        }

        /// <summary>
        /// 이동 처리 후 바라보는 방향을 보정합니다.
        /// </summary>
        private void LateUpdate()
        {
           
        }

        /// <summary>
        /// 비활성화 시 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {

        }

        /// <summary>
        /// 현재 Manager가 파괴되면 Singleton 참조를 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 커스텀 메소드 (Public)

        /// <summary>
        /// 납부 가능 여부를 확인하고 청구서 금액을 지불합니다 (예시 입니다)
        /// </summary>
        public void PayBill()
        {
            if (_state != EGameState.Playing || _paidBillCount >= CMaxBillCount || _money < _billAmount)
            {
                return;
            }

            _money -= _billAmount;
            _paidBillCount++;
            OnMoneyChanged?.Invoke(_money);
        }        

        #endregion

        #region 커스텀 메소드 (Private)

        /// <summary>
        /// Inspector 설정으로 런타임 데이터를 초기화합니다.
        /// </summary>
        private void InitializeGame()
        {
            _money = Mathf.Max(0, _startMoney);
            _currentHp = Mathf.Max(1, _startHp);
            _billAmount = Mathf.Max(1, _billAmount);
            _paidBillCount = 0;
            _moveDirection = Vector3.zero;
            _state = EGameState.Playing;
        }

        #endregion

        #region ETC - Protected Methods

        /// <summary>
        /// 공통 피격 반응을 샘플 전용으로 재정의합니다.
        /// </summary>
        protected override void HandleDamage(int damage)
        {
            base.HandleDamage(damage);
        }

        #endregion
    }

    #region 추상 클래스 예시

    /// <summary>
    /// 종류별 차이만 재정의하며, 상속/추상화를 실제 시스템에 적용합니다.
    /// </summary>
    public abstract class SampleManagerBase : MonoBehaviour, IDamageable
    {
        #region 공용 메소드

        /// <summary>
        /// 종류마다 다른 이동 동작을 구현하도록 강제합니다.
        /// </summary>
        public abstract void Move();

        /// <summary>
        /// 공통 피해 검증 후 재정의 가능한 피격 반응을 호출합니다.
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (IsValidDamage(damage) == false)
            {
                return;
            }

            HandleDamage(damage);
        }

        #endregion
    }

    #endregion
}
