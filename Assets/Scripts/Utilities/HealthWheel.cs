/*
 * HealthWheel.cs
 * Created by: Neo
 * Created on: 15/2/2020 (dd/mm/yy)
 * Created for: Display of enemy health using a bar, or wheel
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthWheel : MonoBehaviour, IPooledObject
{
	[SerializeField] private float _HealthSpeed = 7.5f;

	[HideInInspector] public Transform _Parent = null;
	[HideInInspector] public Vector3 _Offset = Vector3.up;
	[HideInInspector] public bool _InUse = false;
	[HideInInspector] public float _MaxHealth;
	[HideInInspector] public float _CurrentHealth;
	[HideInInspector] public Image _BillboardHealth;
	[HideInInspector] public Canvas _Canvas;

	public Gradient _ColorGradient;

	private CanvasGroup _CanvasGroup;

	// Start is called before the first frame update
	private void Start()
	{
		_BillboardHealth = transform.Find("Health_Display").gameObject.GetComponent<Image>();
		_Canvas = GetComponent<Canvas>();
		_CanvasGroup = GetComponent<CanvasGroup>();
	}

	void IPooledObject.OnObjectSpawn()
	{
		_BillboardHealth.fillAmount = 1;
	}

	bool _Destroying = false;
	IEnumerator IE_CloseAndDestroy()
	{
		_Destroying = true;

		float t = 0;
		float timer = 0.5f;
		while (t < timer)
		{
			_CanvasGroup.alpha = Mathf.Lerp(1, 0, t / timer);
			t += Time.deltaTime;
			yield return null;
		}

		Destroy(gameObject);
	}

	bool _FadedIn = false;
	IEnumerator FadeIn()
	{
		_FadedIn = true;

		float t = 0;
		float timer = 0.5f;
		while (t < timer)
		{
			_CanvasGroup.alpha = Mathf.Lerp(0, 1, t / timer);
			t += Time.deltaTime;
			yield return null;
		}
	}

	// Update is called once per frame
	private void Update()
	{
		// Might be inefficient, optimize if needed
		_Canvas.enabled = _MaxHealth > _CurrentHealth;

		if (_Parent != null)
		{
			transform.position = Vector3.Lerp(transform.position, _Parent.position + _Offset, 0.5f);

			if (_CurrentHealth != _MaxHealth && !_FadedIn)
			{
				StartCoroutine(FadeIn());
			}

			// Smoothly transition between values to avoid hard changing
			_BillboardHealth.fillAmount = Mathf.Lerp(_BillboardHealth.fillAmount, _CurrentHealth / _MaxHealth, _HealthSpeed * Time.deltaTime);
			_BillboardHealth.color = _ColorGradient.Evaluate(_CurrentHealth / _MaxHealth);
		}
		else if (_Destroying == false)
		{
			StartCoroutine(IE_CloseAndDestroy());
		}
	}
}
