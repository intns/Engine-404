using UnityEditor;
using UnityEngine;

public class DisableOnBuild : MonoBehaviour
{
	void Awake()
	{
#if UNITY_EDITOR
		if (!EditorApplication.isPlaying)
		{
			gameObject.SetActive(false);
		}
#else
gameObject.SetActive(false);
#endif
	}
}
