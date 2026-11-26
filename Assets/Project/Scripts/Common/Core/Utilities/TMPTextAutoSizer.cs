using TMPro;
using UnityEngine;

/// <summary>
/// 텍스트 길이와 RectTransform 크기에 따라 글자 크기를 자동 조절
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPTextAutoSizer : MonoBehaviour
{
	#region 인스펙터
	[Header("글자 크기 설정")]
	[SerializeField] private float _minFontSize = 12f;
	[SerializeField] private float _maxFontSize = 36f;

	[Header("탐색 설정")]
	[SerializeField] private int _maxIterations = 12;
	#endregion

	#region 내부 변수
	private TMP_Text _text;
	private RectTransform _rectTransform;

	private string _previousText;
	private Vector2 _previousRectSize;

	private bool _isDirty = true;
    #endregion

    #region 이벤트 함수
    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
		_rectTransform = _text.rectTransform;

		// TMP 자체 Auto Size와 중복되지 않도록 설정
		_text.enableAutoSizing = false;
    }

    private void OnEnable()
    {
		_isDirty = true;
    }

    private void LateUpdate()
    {
        if (_text == null || _rectTransform == null)
		{
			return;
		}

		CheckTextChanged();
		CheckRectSizeChanged();

		if (_isDirty)
		{
			AdjustFontSize();
		}
    }
	#endregion

	#region 커스텀 (private)
	/// <summary>
	/// 텍스트 내용이 변경되었는지 확인
	/// </summary>
	private void CheckTextChanged()
	{
		if (_previousText != _text.text)
		{
			_previousText = _text.text;
			_isDirty = true;
		}
	}

	/// <summary>
	/// 텍스트 영역의 크기가 변경되었는지 확인
	/// </summary>
	private void CheckRectSizeChanged()
	{
		Vector2 currentSize = _rectTransform.rect.size;

		if (_previousRectSize != currentSize)
		{
			_previousRectSize = currentSize;
			_isDirty = true;
		}
	}

	/// <summary>
	/// 현재 텍스트가 들어갈 수 있는 가장 큰 글자 크기 찾기
	/// </summary>
	private void AdjustFontSize()
	{
		float width = _rectTransform.rect.width;
		float height = _rectTransform.rect.height;

        if (width <= 0f || height <= 0f)
        {
			return;
        }

		float minSize = Mathf.Max(1f, _minFontSize);
		float maxSize = Mathf.Max(minSize, _maxFontSize);

		float bestSize = minSize;
		float low = minSize;
		float high = maxSize;

		// 최소 크기에서도 들어가지 않는 경우를 확인
		if (!CanFitText(minSize, width, height))
		{
			_text.fontSize = minSize;
			_isDirty = false;
			return;
		}

		// 이진 탐색으로 가장 큰 글자 크기를 찾음.
		for (int i = 0; i < _maxIterations; i++)
		{
			float middle = (low + high) * 0.5f;

			if (CanFitText(middle, width, high))
			{
				bestSize = middle;
				low = middle;
			}
			else
			{
				high = middle;
			}
		}

		_text.fontSize = bestSize;
		_isDirty = false;
    }

	/// <summary>
	/// 지정한 글자 크기로 텍스트가 영역안에 들어가는지 확인
	/// </summary>
	/// <param name="fontSize"></param>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <returns></returns>
	private bool CanFitText(float fontSize, float width, float height)
	{
		_text.fontSize = fontSize;

        // GetPreferredValues: 현재 글자 크기와 텍스트 기준으로 필요한 영역의 크기 계산
        Vector2 preferredSize = _text.GetPreferredValues(_text.text, width, height);

		return preferredSize.x <= width && preferredSize.y <= height;
	}
	#endregion

}
