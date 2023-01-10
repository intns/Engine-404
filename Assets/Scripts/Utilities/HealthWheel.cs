using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthWheel : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] float _ChangeSpeed = 7.5f;
	[SerializeField] float _FadeTime = 0.5f;
	[SerializeField] Gradient _ColorGradient = null;

	CanvasGroup _CanvasGroup = null;
	Image _BillboardHealth = null;
	Canvas _Canvas = null;

	Vector3 _Offset = Vector3.up;
	Transform _Parent = null;

	float _CurrentHealth = 0.0f;
	float _MaxHealth = 0.0f;

	bool _IsFadeOut = false;
	bool _IsFadeIn = false;
	float _Timer = 0.0f;

	void Awake()
	{
		_BillboardHealth = transform.Find("Health_Display").gameObject.GetComponent<Image>();
		_BillboardHealth.fillAmount = _CurrentHealth;

		_IsFadeIn = false;
		_IsFadeOut = false;

		_Canvas = GetComponent<Canvas>();
		_Canvas.enabled = true;

		_CanvasGroup = GetComponent<CanvasGroup>();
		_CanvasGroup.enabled = true;
		_CanvasGroup.alpha = 0.0f;
	}

	void Update()
	{
		// TODO: Fade away if health hasn't changed in 10 seconds

		// If we've lost health, and we haven't spawned in yet, enable view
		if (!_IsFadeIn && _CurrentHealth < _MaxHealth && _CanvasGroup.alpha == 0.0f)
		{
			_Timer = 0.0f;
			_IsFadeIn = true;
		}

		if (_IsFadeIn)
		{
			if (_Timer <= _FadeTime)
			{
				_CanvasGroup.alpha = MathUtil.EaseIn3(_Timer / _FadeTime);
				_Timer += Time.deltaTime;
			}
			else
			{
				_CanvasGroup.alpha = 1.0f;
				_IsFadeIn = false;
			}
		}

		float fillFrac = _CurrentHealth / _MaxHealth;
		_BillboardHealth.fillAmount = Mathf.Lerp(_BillboardHealth.fillAmount, fillFrac, _ChangeSpeed * Time.deltaTime);
		_BillboardHealth.color = _ColorGradient.Evaluate(fillFrac);

		if (!_IsFadeOut && (_CurrentHealth <= 0 || _Parent == null))
		{
			_Timer = 0.0f;
			_IsFadeOut = true;
		}

		if (_Parent != null)
		{
			transform.position = Vector3.Lerp(transform.position, _Parent.position + _Offset, 0.25f);
		}

		if (_IsFadeOut)
		{
			if (_Timer <= _FadeTime)
			{
				_CanvasGroup.alpha = 1.0f - MathUtil.EaseIn3(_Timer / _FadeTime);
				_Timer += Time.deltaTime;
			}
			else
			{
				_CanvasGroup.alpha = 0.0f;
				Destroy(gameObject); // fucking bugs, working on this for 5 hours, fuck this
			}
		}
	}

	public void Setup(Transform parent, Vector3 offset, Vector3 localScale, float maxHealth)
	{
		_Parent = parent;
		transform.localScale = localScale;

		_Offset = offset;
		_CurrentHealth = _MaxHealth = maxHealth;
	}

	public void SetCurrentHealth(float health)
	{
		_CurrentHealth = health;
	}
}
