using Cinemachine.Utility;
using UnityEngine;

public class Nectar : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] float _HorizontalDamageAnimFactor = 1.0f;
	[SerializeField] float _VerticalDamageAnimFactor = 1.0f;
	[Space]
	[SerializeField, Tooltip("If not small sips per piki, then drink whole")]
	bool _UseSips = true;
	
	[SerializeField] int _SipsUntilDeath = 10;

	[Header("Debugging")]
	[SerializeField] Transform _Transform = null;
	[SerializeField] Vector3 _StartSize = Vector3.zero;
	[Space]
	[SerializeField] float _InteractionTimer = 0.0f;
	[SerializeField] bool _Interacted = false;
	[Space]
	[SerializeField] int _SipsLeft = 0;
	[SerializeField] float _DrinkTimer = 0.0f;
	[SerializeField] bool _IsBeingDrunk = false;

	public static float NECTAR_DRINK_TIME = 5.0f;

	void Awake()
	{
		_Transform = transform;
		_StartSize = _Transform.localScale;

		_SipsLeft = _SipsUntilDeath;
	}

	void Update()
	{
		if (_Interacted)
		{
			if (_InteractionTimer == 0.0f)
			{
				if (_Interacted)
				{
					_InteractionTimer += Time.deltaTime;
				}

				_Interacted = false;
				return;
			}

			float scaleDuration = 1.0f;

			_InteractionTimer += Time.deltaTime;

			float horizontalMod = 0.0f;
			if (_InteractionTimer <= scaleDuration)
			{
				float t = _InteractionTimer / scaleDuration;
				horizontalMod = (1.0f - t) * Mathf.Sin(t * MathUtil.M_TAU) * _VerticalDamageAnimFactor;
			}
			else
			{
				_InteractionTimer = 0.0f;
			}

			float xzScale = horizontalMod * (_HorizontalDamageAnimFactor * 0.2f);
			_Transform.localScale = new(_StartSize.x - xzScale, (horizontalMod * 0.25f) + _StartSize.y, _StartSize.z - xzScale);
		}

		Vector3 targetScale = _StartSize;
		if (_UseSips)
		{
			targetScale *= _SipsLeft / (float)_SipsUntilDeath;
		}
		else if (_IsBeingDrunk)
		{
			_DrinkTimer += Time.deltaTime;
			targetScale *= 1 - (_DrinkTimer / NECTAR_DRINK_TIME);
		}
		_Transform.localScale = Vector3.Lerp(_Transform.localScale, targetScale, 5 * Time.deltaTime);
		
		if (targetScale.sqrMagnitude <= 0.05f)
		{
			Destroy(gameObject);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_SipsLeft <= 0 && _UseSips)
		{
			return;
		}

		if (other.CompareTag("Pikmin") && other.TryGetComponent(out PikminAI ai))
		{
			if (ai.NectarStart(_Transform))
			{
				_SipsLeft--;
				_IsBeingDrunk = true;
			}
			else
			{
				_Interacted = true;
			}
		}
		else if (other.CompareTag("Player"))
		{
			_Interacted = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (_SipsLeft <= 0 && _UseSips)
		{
			return;
		}

		if (other.CompareTag("Pikmin") && other.TryGetComponent(out PikminAI ai)
			&& ai._CurrentState != PikminStates.SuckNectar && ai.NectarStart(_Transform))
		{
			_SipsLeft--;
			_IsBeingDrunk = true;
		}
	}
}
