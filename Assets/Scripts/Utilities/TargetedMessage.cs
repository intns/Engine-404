using UnityEngine;

public class TargetedMessage : MonoBehaviour
{
	[SerializeField] GameObject _Target;

	public void SendTargetedMessage(string target)
	{
		if (_Target != null)
		{
			_Target.SendMessage(target, SendMessageOptions.RequireReceiver);
		}
	}
}
