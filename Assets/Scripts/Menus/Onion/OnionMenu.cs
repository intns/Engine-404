using UnityEngine;
using UnityEngine.UI;

public class OnionMenu : MonoBehaviour
{
	[System.Serializable]
	public class pikminCounters
	{

		public GameObject counterObject;
		public bool active;
		public Text squadNumber;
		public Text onionNumber;

		public int debugSquad;
		public int debugOnion;
	}

	public pikminCounters[] m_pikminCounters;

	public Text fieldText;
	public int debugField;

	// Start is called before the first frame update
	private void Start()
	{
		foreach (pikminCounters target in m_pikminCounters)
		{

			target.counterObject.SetActive(target.active);
			//Set counter positions
		}
	}

	// Update is called once per frame
	private void Update()
	{

		foreach (pikminCounters target in m_pikminCounters)
		{

			target.squadNumber.text = "" + target.debugSquad; //Help me get the amount of that type of pikmin in squad
			target.onionNumber.text = "" + target.debugOnion; //And in onion

		}

		fieldText.text = "<size=6>Field</size>\n" + debugField;

	}
}
