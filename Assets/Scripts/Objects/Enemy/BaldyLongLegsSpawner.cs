using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaldyLongLegsSpawner : MonoBehaviour
{
	[SerializeField] AnimationCurve _SpawnInCurve;
	[SerializeField] GameObject _LongLegsObj;
	bool _Spawning = false;
	Vector3 _SpawnPosition;

	private void Awake()
	{
		_LongLegsObj.SetActive(false);
		_SpawnPosition = transform.position + Vector3.up * 25;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!_Spawning && other.CompareTag("Player"))
		{
			_LongLegsObj.transform.position = _SpawnPosition;
			StartCoroutine(IE_SpawnIn());
		}
	}

	IEnumerator IE_SpawnIn()
	{
		yield return null;

		_Spawning = true;
		_LongLegsObj.SetActive(true);

		float timer = 0;
		while (timer < 1.5f)
		{
			_LongLegsObj.transform.position = Vector3.Lerp(_SpawnPosition, transform.position - (Vector3.up*5),
				_SpawnInCurve.Evaluate(timer / 1.5f));
			timer += Time.deltaTime;
			yield return null;
		}

		enabled = false;
		yield return null;
	}
}
