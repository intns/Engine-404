using System.Collections;
using Demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] TextMeshProUGUI _DayText;
	[SerializeField] TextMeshProUGUI _SquadText;
	[SerializeField] TextMeshProUGUI _AreaText;
	[SerializeField] TextMeshProUGUI _TotalText;
	[SerializeField] Image _HealthWheel;
	[SerializeField] Gradient _ColorGradient;

	[SerializeField] Image _CurrentPikminImage;
	[SerializeField] Sprite[] _PikminImages;

	[SerializeField] CanvasGroup _CanvasGroup;

	[SerializeField] bool _DisplayValues;
	Animation _AreaTextAnimation;
	Animation _DayTextAnimation;

	int _InFieldAmount = -1;
	int _InSquadAmount = -1;

	Animation _PikminImageAnimation;

	Animation _SquadTextAnimation;
	float _TickTimer;
	int _TotalAmount = -1;
	Animation _TotalTextAnimation;

	void Awake()
	{
		_SquadText.text = "000";
		_AreaText.text = "000";
		_TotalText.text = "0000";

		_CurrentPikminImage.color = Color.clear;
		_PikminImageAnimation = _CurrentPikminImage.GetComponent<Animation>();

		_SquadTextAnimation = _SquadText.GetComponent<Animation>();
		_AreaTextAnimation = _AreaText.GetComponent<Animation>();
		_TotalTextAnimation = _TotalText.GetComponent<Animation>();
		_DayTextAnimation = _DayText.GetComponent<Animation>();
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

		if (_TickTimer > 0.05f)
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

	public void FadeInUI(bool shouldUpdateOnFinish = false, float totalTime = 1.0f)
	{
		StartCoroutine(FadeInCanvas(shouldUpdateOnFinish, totalTime));
	}

	public void FadeOutUI(float totalTime = 1.0f)
	{
		if (totalTime == 0.0f)
		{
			_CanvasGroup.alpha = 0.0f;
			_DisplayValues = false;
			return;
		}

		StartCoroutine(FadeOutCanvas(totalTime));
	}


	IEnumerator FadeInCanvas(bool shouldUpdateOnFinish, float totalTime = 1.0f)
	{
		float t = 0;
		_DisplayValues = false;

		while (t <= totalTime)
		{
			t += Time.deltaTime;

			_CanvasGroup.alpha = MathUtil.EaseIn4(t / totalTime);
			yield return null;
		}

		_DisplayValues = true;

		if (shouldUpdateOnFinish)
		{
			UpdateFullUI();
		}
	}

	IEnumerator FadeOutCanvas(float totalTime)
	{
		float t = 0;
		_DisplayValues = true;

		while (t <= totalTime)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = 1 - MathUtil.EaseIn4(t / totalTime);
			yield return null;
		}

		_DisplayValues = false;
	}

	#region Public functions

	public void UpdateUI()
	{
		int newInSquad = PikminStatsManager.GetTotalPikminInSquad();
		int newOnField = PikminStatsManager.GetTotalPikminOnField();
		int newTotal = PikminStatsManager.GetTotalPikminInAllOnions() + newOnField;

		if (_InSquadAmount != newInSquad)
		{
			_SquadTextAnimation.Stop();
			_SquadTextAnimation.Play();
		}

		if (_InFieldAmount != newOnField)
		{
			_AreaTextAnimation.Stop();
			_AreaTextAnimation.Play();
		}

		if (_TotalAmount != newTotal)
		{
			_TotalTextAnimation.Stop();
			_TotalTextAnimation.Play();
		}

		_InSquadAmount = newInSquad;
		_InFieldAmount = newOnField;
		_TotalAmount = newTotal;

		_SquadText.text = _InSquadAmount.ToString("D3");
		_AreaText.text = _InFieldAmount.ToString("D3");
		_TotalText.text = _TotalAmount.ToString("D4");
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
		_DayTextAnimation.Play();
	}

	#endregion
}
