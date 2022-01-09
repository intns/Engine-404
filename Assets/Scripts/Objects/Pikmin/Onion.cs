/*
 * Onion.cs
 * Created by: Ambrosia
 * Created on: 12/4/2020 (dd/mm/yy)
 * Created for: Needing an object to store our Pikmin in
 */

using UnityEngine;

public class Onion : MonoBehaviour {
  public enum OnionType {
    Classic, // When first finding an onion, it will be this
    Master // Main onion that has the combination of other onions
  }

  [Header ("Debug")]
  [SerializeField] private GameObject _Pikmin = null;

  [Header ("Settings")]
  [SerializeField] private OnionType _Type = OnionType.Classic;
  public PikminColour _PikminColour = PikminColour.Red;
  [SerializeField] private LayerMask _MapMask = 0;

  [Header ("Dispersal")]
  [SerializeField] private float _DisperseRadius = 2;

  public Transform _CarryEndpoint = null;
  private bool _CanUse = false;
  private bool _InMenu = false;

  private void Update () {
    if (_CanUse && Input.GetButtonDown ("A Button")) {
      // Set the menu to the opposite of what it just was (true -> false || false -> true)
      // TODO: Pause player input and the world
      _InMenu = !_InMenu;

      if (_InMenu == false) {
        print ("Closing Onion menu");
        Globals._Player._MovementController._Paralysed = false;
        // TODO: Play animation where the UI goes away
        return;
      }

      print ("Opening Onion menu");
      Globals._Player._MovementController._Paralysed = true;
    }

    // Handle in-menu input processing
    if (_InMenu) {
      // TODO: UI Animations for changing the text value
      print ($"There are currently {PikminStatsManager.GetInOnion(_PikminColour)} Pikmin in the onion");
    }
  }

  private void OnTriggerEnter (Collider other) {
    if (other.CompareTag ("Player")) {
      _CanUse = true;
    }
  }

  private void OnTriggerExit (Collider other) {
    if (other.CompareTag ("Player")) {
      _CanUse = false;
    }
  }

  private void OnDrawGizmosSelected () {
    if (Physics.Raycast (transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore)) {
      for (int i = 0; i < 50; i++) {
        Vector2 offset = MathUtil.PositionInUnit (50, i);
        Gizmos.DrawSphere (hit.point + MathUtil.XZToXYZ (offset) * _DisperseRadius, 1);
      }
    }
  }

  public void EnterOnion (int toProduce) {
    // Create seeds to pluck...
    if (Physics.Raycast (transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore)) {
      for (int i = 0; i < toProduce; i++) {
        Vector2 offset = MathUtil.PositionInUnit (100, Random.Range (0, 100));
        Instantiate (_Pikmin, hit.point + MathUtil.XZToXYZ (offset) * _DisperseRadius, Quaternion.identity);
      }
    }
  }
}
