using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaldyLongLegsSpawner : MonoBehaviour
{
	[SerializeField] AnimationCurve _SpawnInCurve;
	[SerializeField] GameObject _LongLegsObj;
	[SerializeField] float _SpawnOffsetY = 100;
	BaldyLongLegs _BLL = null;
	bool _Spawned = false;
	Vector3 _SpawnPosition;

	private void Awake()
	{
		_LongLegsObj.SetActive(false);
		_BLL = _LongLegsObj.GetComponent<BaldyLongLegs>();
		_SpawnPosition = transform.position + Vector3.up * _SpawnOffsetY;
	}

	private void Update()
	{
		if (_BLL._Target == null)
		{
			_BLL._Target = transform;
		}
	}

	private void OnTriggerEnter(Collider other)
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

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player") || other.CompareTag("Pikmin"))
		{
			_BLL._Target = transform;
		}
	}

	private void OnTriggerStay(Collider other)
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

		float timer = 0;
		while (timer < 1.5f)
		{
			_LongLegsObj.transform.position = Vector3.Lerp(_SpawnPosition, transform.position - (Vector3.up * 5),
				_SpawnInCurve.Evaluate(timer / 1.5f));
			timer += Time.deltaTime;
			yield return null;
		}

		yield return null;
	}
}
