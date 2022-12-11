using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WP_Test : MonoBehaviour
{
	[SerializeField] Transform _StartPosition;
	[SerializeField] Transform _EndPosition;
	Seeker _Pathfinder = null;

	void Start()
	{
		_Pathfinder = GetComponent<Seeker>();
		StartCoroutine(PATH());
	}

	IEnumerator PATH()
	{
		Path p = _Pathfinder.StartPath(_StartPosition.position, _EndPosition.position);
		yield return p.WaitForPath();

		Debug.Log(p.vectorPath.Count);

		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.transform.position = _StartPosition.position;

		int currIdx = 0;
		while (MathUtil.DistanceTo(go.transform.position, _EndPosition.position) > 0.5f)
		{
			go.transform.position = Vector3.Lerp(go.transform.position, p.vectorPath[currIdx], 5 * Time.deltaTime);

			if (Vector3.Distance(go.transform.position, p.vectorPath[currIdx]) < 0.5f)
			{
				currIdx = Mathf.Min(currIdx, p.vectorPath.Count);
			}
		}
	}
}
