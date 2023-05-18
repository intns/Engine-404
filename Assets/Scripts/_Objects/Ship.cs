using System.Collections;
using Demo;
using UnityEngine;
using UnityEngine.VFX;

public class Ship : MonoBehaviour, ICarryObjectSuck
{
	public static Ship _Instance;

	[Header("Components")]
	[SerializeField] VisualEffect _FireVFX;

	[Header("Treasures")]
	[SerializeField] Transform _SuctionEndPosition;
	[SerializeField] float _SuctionDuration = 1.5f;

	[HideInInspector]
	public Transform _Transform;

	#region Public Functions

	public Vector3 GetSuctionPosition()
	{
		return _SuctionEndPosition.position;
	}

	/// <summary>
	///   Starts / Stops flames underneath ship
	/// </summary>
	/// <param name="state">Whether to start or stop the flames</param>
	public void SetEngineFlamesVFX(bool state)
	{
		if (state)
		{
			_FireVFX.Play();
		}
		else
		{
			_FireVFX.Stop();
		}
	}

	public void StartSuck(PikminCarryObject obj)
	{
		StartCoroutine(IE_SuctionAnimation(obj.gameObject));
	}

	IEnumerator IE_SuctionAnimation(GameObject obj)
	{
		yield return null;

		float t = 0;
		Vector3 origin = obj.transform.position;
		Vector3 originScale = obj.transform.localScale;

		while (t <= _SuctionDuration)
		{
			obj.transform.position = Vector3.Lerp(
				origin,
				_SuctionEndPosition.position,
				MathUtil.EaseIn4(t / _SuctionDuration)
			);
			obj.transform.localScale = Vector3.Lerp(originScale, Vector3.zero, MathUtil.EaseIn3(t / _SuctionDuration));

			t += Time.deltaTime;
			yield return null;
		}

		SceneShipData scenePart = ShipManager._Instance._SceneParts.Find(part => part._Object == obj);

		if (scenePart != null)
		{
			scenePart._Data._Collected = true;
		}
		else
		{
			Debug.LogError("Scene part data not found for the gameObject: " + gameObject.name);
		}

		Destroy(obj);

		yield return new WaitForEndOfFrame();
	}

	#endregion


	#region Unity Functions

	void OnEnable()
	{
		_Instance = this;
		SetEngineFlamesVFX(false);
	}

	void Awake()
	{
		_Transform = transform;
	}

	#endregion
}
