using UnityEngine;

public class DontDestroy : MonoBehaviour {
  private void Awake () {
    DontDestroyOnLoad (gameObject);
  }
}
