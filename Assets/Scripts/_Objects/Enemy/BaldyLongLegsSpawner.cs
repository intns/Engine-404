using System.Collections;
using UnityEngine;

public class BaldyLongLegsSpawner : MonoBehaviour
{
	[SerializeField] AnimationCurve _SpawnInCurve;
	[SerializeField] GameObject _LongLegsObj;
	[SerializeField] float _SpawnOffsetY = 100;
	BaldyLongLegs _BLL = null;
	bool _Spawned = false;
	Vector3 _SpawnPosition;

	void Awake()
	{
		_LongLegsObj.SetActive(false);
		_BLL = _LongLegsObj.GetComponent<BaldyLongLegs>();
		_SpawnPosition = transform.position + Vector3.up * _SpawnOffsetY;
	}

	void Update()
	{
		if (GameManager.IsPaused)
		{
			return;
		}

		if (_BLL._Target == null)
		{
			_BLL._Target = transform;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") || other.CompareTag("Pikmin"))
		{
			if (!_Spawned)
			{
				_LongLegsObj.transform.position = _SpawnPosition;
				StartCoroutine(IE_SpawnIn());
			}

			_BLL._Target = other.transform;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player") || other.CompareTag("Pikmin"))
		{
			_BLL._Target = transform;
		}
	}

	void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player") || other.CompareTag("Pikmin"))
		{
			_BLL._Target = other.transform;
		}
	}

	IEnumerator IE_SpawnIn()
	{
		yield return null;

		_Spawned = true;
		_LongLegsObj.SetActive(true);

		yield return null;
	}
}
