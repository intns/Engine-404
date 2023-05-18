using System.Collections;
using _Demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
	[SerializeField] TextMeshProUGUI _DayText;
	[SerializeField] TextMeshProUGUI _SquadText;
	[SerializeField] TextMeshProUGUI _AreaText;
	[SerializeField] Image _HealthWheel;
	[SerializeField] Gradient _ColorGradient;

	[SerializeField] Image _CurrentPikminImage;
	[SerializeField] Sprite[] _PikminImages;

	[SerializeField] CanvasGroup _CanvasGroup;

	[SerializeField] bool _DisplayValues;
	int _InFieldAmount = -1;
	int _InSquadAmount = -1;
	Animation _PikminImageAnimation;
	float _TickTimer;

	void Awake()
	{
		_SquadText.text = string.Empty;
		_AreaText.text = string.Empty;

		_CurrentPikminImage.color = Color.clear;
		_PikminImageAnimation = _CurrentPikminImage.GetComponent<Animation>();
	}

	void Start()
	{
		_DayText.text = SaveData._CurrentData._Day.ToString();
	}

	void Update()
	{
		if (!_DisplayValues)
		{
			return;
		}

		_TickTimer += Time.deltaTime;

		if (_TickTimer > 0.2f)
		{
			UpdateUI();
			_TickTimer = 0.0f;
		}

		float currHealth = Player._Instance.GetCurrentHealth();
		float maxHealth = Player._Instance.GetMaxHealth();
		float ratio = currHealth / maxHealth;

		_HealthWheel.fillAmount = Mathf.Lerp(_HealthWheel.fillAmount, ratio, 2.0f * Time.deltaTime);
		_HealthWheel.color = _ColorGradient.Evaluate(ratio);
	}

	public void FadeInUI(bool shouldUpdateOnFinish = false)
	{
		StartCoroutine(FadeInCanvas(shouldUpdateOnFinish));
	}

	public void FadeOutUI()
	{
		StartCoroutine(FadeOutCanvas());
	}


	IEnumerator FadeInCanvas(bool shouldUpdateOnFinish)
	{
		float t = 0;
		float time = 1;
		_DisplayValues = false;

		while (t <= time)
		{
			t += Time.deltaTime;

			_CanvasGroup.alpha = MathUtil.EaseIn4(t / time);
			yield return null;
		}

		_DisplayValues = true;

		if (shouldUpdateOnFinish)
		{
			UpdateFullUI();
		}
	}

	IEnumerator FadeOutCanvas()
	{
		float t = 0;
		float time = 1;
		_DisplayValues = true;

		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = 1 - MathUtil.EaseIn4(t / time);
			yield return null;
		}

		_DisplayValues = false;
	}

	#region Public functions

	public void UpdateUI()
	{
		int newInSquad = PikminStatsManager.GetTotalPikminInSquad();
		int newOnField = PikminStatsManager.GetTotalPikminOnField();

		if (_InSquadAmount != newInSquad)
		{
			_SquadText.GetComponent<Animation>().Stop();
			_SquadText.GetComponent<Animation>().Play();
		}

		if (_InFieldAmount != newOnField)
		{
			_AreaText.GetComponent<Animation>().Stop();
			_AreaText.GetComponent<Animation>().Play();
		}

		_InSquadAmount = newInSquad;
		_InFieldAmount = newOnField;

		_SquadText.text = _InSquadAmount.ToString();
		_AreaText.text = _InFieldAmount.ToString();
		_DayText.text = SaveData._CurrentData._Day.ToString();

		PikminColour colour = Player._Instance._PikminController._SelectedThrowPikmin;

		if (colour != PikminColour.Size)
		{
			_CurrentPikminImage.color = Color.white;
			_CurrentPikminImage.sprite = _PikminImages[(int)colour];
		}
		else
		{
			_CurrentPikminImage.color = Color.clear;
		}
	}

	public void UpdateFullUI()
	{
		UpdateUI();

		_PikminImageAnimation.Play();
		_DayText.GetComponent<Animation>().Play();
	}

	#endregion
}
