using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CarryText : MonoBehaviour
{
	[Header("Settings")]
	public float _HeightOffset = 5;
	[SerializeField] private float _FadeDuration = 1;
	[SerializeField] private TextMeshPro _TMP = null;
	[HideInInspector] public Transform _FollowTarget = null;
	[SerializeField] private Color _LastColor = Color.clear;
	[SerializeField] private Color _TargetColor = Color.clear;
	[SerializeField] private float _TimeSinceLastChange = 0;

	private void Awake()
	{
		_TMP.text = "";
		_LastColor = _TargetColor = _TMP.color = Color.clear;
	}

	private void Update()
	{
		if (_FollowTarget == null)
		{
			return;
		}
		transform.position = _FollowTarget.position + Vector3.up * _HeightOffset;

		_TimeSinceLastChange = Mathf.Min(_TimeSinceLastChange + Time.deltaTime, _FadeDuration);
		_TMP.color = Color.Lerp(_LastColor, _TargetColor, MathUtil.EaseOut2(_TimeSinceLastChange / _FadeDuration));
	}

	public void SetText(int amount, int max)
	{
		_TMP.text = $"{amount} / {max}";
	}

	public void UpdateColor(List<PikminAI> clients)
	{
		_TimeSinceLastChange = 0;
		_LastColor = _TMP.color;
		_TargetColor =
			clients.Count == 0 ? Color.clear :
			GameUtil.GetMajorityColour(clients) switch
			{
				PikminColour.Red => Color.red,
				PikminColour.Yellow => Color.yellow,
				PikminColour.Blue => new Color(0, .06f, 1),
				_ => Color.white,
			};
	}

	public void Destroy()
	{
		_FollowTarget = null;
		_TargetColor = Color.clear;
		Destroy(gameObject, _FadeDuration);
	}
}
