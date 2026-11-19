using System.Collections.Generic;
using UnityEngine;

public class RewardTextManager : Singleton<RewardTextManager>
{
    #region 인스펙터
    [Header("설정")]
    [SerializeField] private RewardText _prefab;
    [SerializeField] protected Transform _canvasTransform;
    [SerializeField] protected int _initialPoolSize = 20;
    #endregion

    #region 내부 변수
    private readonly Queue<RewardText> _pool = new Queue<RewardText>();
    private readonly HashSet<RewardText> _activeTexts = new HashSet<RewardText>();
    private bool _poolInitialized;
    #endregion

    #region 이벤트 함수
    protected override void OnSingletonAwake()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }
        ClearPool();
    }
    #endregion

    #region 커스텀 함수(public)
    public void ShowReward(int reward, Vector3 worldPos)
    {
        ShowReward(reward, worldPos, false);
    }

    public void ShowReward(int reward, Vector3 worldPos, bool isJackpot)
    {
        if (!IsRewardTextEnabled() || _prefab == null)
        {
            return;
        }

        InitializePool();
        RewardText text = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_prefab, _canvasTransform);
        _activeTexts.Add(text);

        Vector3 randomOffset = new Vector3(0, Random.Range(-0.1f, -0.05f), 0f);

        text.transform.position = worldPos + randomOffset;
        text.Init(reward, isJackpot);
    }

    public void ReturnToPool(RewardText text)
    {
        if (text == null || !text.gameObject.activeSelf)
        {
            return;
        }

        if (!IsRewardTextEnabled())
        {
            text.gameObject.SetActive(false);
            Destroy(text.gameObject);
            return;
        }

        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }

    private void InitializePool()
    {
        if (_poolInitialized)
        {
            return;
        }

        _poolInitialized = true;
        if (_canvasTransform == null)
        {
            _canvasTransform = transform;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            RewardText item = Instantiate(_prefab, _canvasTransform);
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
    }

    private bool IsRewardTextEnabled()
    {
        return SettingsManager.Instance == null || SettingsManager.Instance.RewardTextEnabled;
    }

    private void HandleSettingsChanged()
    {
        if (!IsRewardTextEnabled())
        {
            ClearPool();
        }
    }

    private void ClearPool()
    {
        foreach (RewardText text in _pool)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
                Destroy(text.gameObject);
            }
        }
        _pool.Clear();

        foreach (RewardText text in _activeTexts)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
                Destroy(text.gameObject);
            }
        }
        _activeTexts.Clear();
        _poolInitialized = false;
    }
    #endregion

}
