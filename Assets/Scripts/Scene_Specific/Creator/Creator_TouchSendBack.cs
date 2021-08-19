using UnityEngine;
using UnityEngine.SceneManagement;

public class Creator_TouchSendBack : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Globals._FadeManager.FadeIn(1, new System.Action(() => { SceneManager.LoadScene(0); }));
		}
	}
}
