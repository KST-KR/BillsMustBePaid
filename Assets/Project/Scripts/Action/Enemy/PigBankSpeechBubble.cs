using System.Collections;
using UnityEngine;
using TMPro;

public class PigBankSpeechBubble : MonoBehaviour
{
    #region 인스펙터
    [Header("UI 연결")]
    [SerializeField] private GameObject _speechBubble;
    [SerializeField] private TextMeshProUGUI _Text;

    [Header("대사 설정")]
    [TextArea]
    [SerializeField] private string[] _textLogues;
    [SerializeField] private float _minInterval = 10f;
    [SerializeField] private float _maxInterval = 30f;
    [SerializeField] private float _textHoldingTime = 2f;
    #endregion

    #region 내부 변수
    private Coroutine _speechCoroutine;
    private Camera _mainCam;
    #endregion

    #region 이벤트 함수
    private void OnEnable()
    {
        if(_speechBubble != null)
        {
            _speechBubble.SetActive(false);
        }

        if(_textLogues != null && _textLogues.Length > 0)
        {
            _speechCoroutine = StartCoroutine(SpeechRoutine()); 
        }
    }

    private void Start()
    {
        _mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if(_speechBubble != null && _speechBubble.activeSelf && _mainCam != null)
        {
            _speechBubble.transform.rotation = _mainCam.transform.rotation;
        }
    }

    private void OnDisable()
    {
        if(_speechCoroutine != null)
        {
            StopCoroutine(_speechCoroutine);
            _speechCoroutine = null;
        }
    }
    #endregion

    #region 커스텀 함수(private)
    private IEnumerator SpeechRoutine()
    {
        while (true)
        {
            // 랜덤 시간 대기 
            float waitTime = Random.Range(_minInterval, _maxInterval);
           
            while(waitTime > 0f)
            {
                waitTime -= Time.deltaTime;
                yield return null;
            }

            // 게임 플레이 중일 떄만 출력
            if(GameManager.Instance != null && GameManager.Instance.State != EGameState.Playing)
            {
                continue;
            }

            // 랜덤 대사 출력
            string line = _textLogues[Random.Range(0, _textLogues.Length)];
            _Text.text = line;
            _speechBubble.SetActive(true);

            float holdTime = _textHoldingTime;

            while (holdTime > 0f)
            {
                holdTime -= Time.deltaTime;
                yield return null;
            }

            _speechBubble.SetActive(false);
        }
    }
    #endregion
}
