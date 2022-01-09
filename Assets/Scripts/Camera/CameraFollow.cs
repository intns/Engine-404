/*
 * CameraFollow.cs
 * Created by: Neo, Ambrosia
 * Created on: 6/2/2020 (dd/mm/yy)
 * Created for: following a target with incrementable offset and field of view
 */

using System.Collections;
using UnityEngine;

[System.Serializable]
public class CameraHolder {
  public float _FOV = 75f;
  public Vector2 _Offset = Vector2.one;
}

[RequireComponent (typeof (AudioSource))]
public class CameraFollow : MonoBehaviour {
  [Header ("Components")]
  [SerializeField] private CameraHolder[] _DefaultHolders = null;
  [SerializeField] private CameraHolder[] _TopViewHolders = null;
  private Camera _MainCamera = null;

  [Header ("Audio")]
  [SerializeField] private AudioClip _ChangeZoomAudio = null;

  [Header ("Movement / Camera Specific")]
  [SerializeField] private float _FollowSpeed = 1;
  [SerializeField] private float _OrbitChangeSpeed = 1;
  [SerializeField] private float _FOVChangeSpeed = 1;

  [Header ("Movement Correction")]
  [SerializeField] private float _HeightChangeSpeed = 1;
  [SerializeField] private float _DistForHeightChange = 1;
  [SerializeField] private float _EasingHeightOffset = 1; // The offset of the sphere used,
  [SerializeField] private float _HeightSphereRadius = 1; // The radius of the sphere used to check if there's a platform higher than what we're currently on

  [Header ("Rotation")]
  [SerializeField] private float _LookAtRotationSpeed = 1;

  [Header ("Controlled Rotation")]
  [SerializeField] private float _RotateAroundSpeed = 1;
  [SerializeField] private float _TriggerRotationSpeed = 1;
  [SerializeField] private float _CameraResetLength = 1;
  [SerializeField] private float _CameraResetCooldown = 1;

  [Header ("Miscellaneous")]
  [SerializeField] private LayerMask _MapLayer = 0;
  private CameraHolder _CurrentHolder;
  private AudioSource _AudioSource;
  private Transform _PlayerPosition;
  private float _OrbitRadius;
  private float _GroundOffset;
  private float _CurrentRotation;
  private float _ResetCooldownTimer;
  private int _HolderIndex;
  private bool _TopView = false;
  private bool _Apply = true;

  private CameraFollow () {
    // Movement / Camera Specific
    _FollowSpeed = 5;
    _FOVChangeSpeed = 2;
    _OrbitChangeSpeed = 2;

    // Movement Correction
    _HeightChangeSpeed = 2;
    _DistForHeightChange = Mathf.Infinity;
    _EasingHeightOffset = 2.5f;
    _HeightSphereRadius = 2;

    // Rotation
    _LookAtRotationSpeed = 5;

    // Controlled Rotation
    _RotateAroundSpeed = 4;
    _CameraResetLength = 2;
    _TriggerRotationSpeed = 2;

    // Non-exposed
    _CurrentRotation = 0;
    _ResetCooldownTimer = 0;
  }

  private void Start () {
    _MainCamera = Camera.main;
    _PlayerPosition = Globals._Player.transform;
    _AudioSource = GetComponent<AudioSource> ();

    if (_DefaultHolders.Length != _TopViewHolders.Length) {
      Debug.LogError ("Top View holders must have the same length as default holders!");
      Debug.Break ();
    }

    // Calculate the middle of the camera array, and access variables from the middle
    _HolderIndex = Mathf.FloorToInt (_DefaultHolders.Length / 2);
    _CurrentHolder = _DefaultHolders[_HolderIndex];
    _OrbitRadius = _CurrentHolder._Offset.x;
    _GroundOffset = _CurrentHolder._Offset.y;
  }

  private void Update () {
    // Rotate the camera to look at the Player
    transform.rotation = Quaternion.Lerp (transform.rotation,
      Quaternion.LookRotation (_PlayerPosition.position - transform.position),
      _LookAtRotationSpeed * Time.deltaTime);

    ApplyCurrentHolder ();
    HandleControls ();
  }

  /// <summary>
  /// Applies the CurrentHolder variable to the Camera's variables
  /// </summary>
  private void ApplyCurrentHolder () {
    // Calculate the offset from the ground using the players current position and our additional Y offset
    float groundOffset = _CurrentHolder._Offset.y + _PlayerPosition.position.y;
    // Store the orbit radius in case we need to alter it when moving onto a higher plane
    float orbitRadius = _CurrentHolder._Offset.x;
    if (Physics.SphereCast (transform.position + (Vector3.up * _EasingHeightOffset),
        _HeightSphereRadius,
        Vector3.down,
        out RaycastHit hit,
        _DistForHeightChange,
        _MapLayer)) {
      float offset = Mathf.Abs (_PlayerPosition.position.y - hit.point.y);
      groundOffset += offset;
      orbitRadius += offset / 1.5f;
    }

    // Smoothly change the OrbitRadius, GroundOffset and the Camera's field of view
    _MainCamera.fieldOfView = Mathf.Lerp (_MainCamera.fieldOfView, _CurrentHolder._FOV, _FOVChangeSpeed * Time.deltaTime);
    _OrbitRadius = Mathf.Lerp (_OrbitRadius, orbitRadius, _OrbitChangeSpeed * Time.deltaTime);
    _GroundOffset = Mathf.Lerp (_GroundOffset, groundOffset, _HeightChangeSpeed * Time.deltaTime);

    if (!_Apply) {
      return;
    }

    // Calculates the position the Camera wants to be in, using Ground Offset and Orbit Radius
    Vector3 targetPosition = (transform.position - _PlayerPosition.position).normalized *
      Mathf.Abs (_OrbitRadius) +
      _PlayerPosition.position;

    targetPosition.y = _GroundOffset;

    transform.position = Vector3.Lerp (transform.position, targetPosition, _FollowSpeed * Time.deltaTime);
  }

  /// <summary>
  /// Handles every type of control the Player has over the Camera
  /// </summary>
  private void HandleControls () {
    // Check if we're holding either the Left or Right trigger and
    // rotate around the player using TriggerRotationSpeed if so
    if (Input.GetButton ("Right Trigger")) {
      RotateView (-_TriggerRotationSpeed * Time.deltaTime);
    }
    else if (Input.GetButton ("Left Trigger")) {
      RotateView (_TriggerRotationSpeed * Time.deltaTime);
    }

    // As we've let go of the triggers, reset the desired new rotation
    if (Input.GetButtonUp ("Right Trigger") || Input.GetButtonUp ("Left Trigger")) {
      _CurrentRotation = 0;
    }

    if (Input.GetButtonDown ("Right Bumper")) {
      _HolderIndex++;
      ApplyChangedZoomLevel (_TopView ? _TopViewHolders : _DefaultHolders);
    }
    if (Input.GetButtonDown ("Left Stick Click")) {
      _TopView = !_TopView; // Invert the TopView 
      ApplyChangedZoomLevel (_TopView ? _TopViewHolders : _DefaultHolders);
    }

    if (Input.GetButtonDown ("Left Bumper") &&
      _ResetCooldownTimer <= 0 &&
      _Apply) {
      float yDif = transform.eulerAngles.y - Globals._Player._MovementController._RotationBeforeIdle.eulerAngles.y;
      if (Mathf.Abs (yDif) >= 0.5f) {
        StartCoroutine (ResetCamOverTime (_CameraResetLength));
      }
    }

    if (_ResetCooldownTimer > 0) {
      _ResetCooldownTimer -= Time.deltaTime;
    }
  }

  /// <summary>
  /// Rotates the camera using a given angle around the Player
  /// </summary>
  /// <param name="angle"></param>
  private void RotateView (float angle) {
    _CurrentRotation = Mathf.Lerp (_CurrentRotation, angle, _RotateAroundSpeed * Time.deltaTime);
    transform.RotateAround (_PlayerPosition.position, Vector3.up, _CurrentRotation);
  }

  /// <summary>
  /// Changes zoom level based on the holder index, and plays audio
  /// </summary>
  /// <param name="currentHolder"></param>
  private void ApplyChangedZoomLevel (CameraHolder[] currentHolder) {
    _AudioSource.PlayOneShot (_ChangeZoomAudio);

    if (_HolderIndex > currentHolder.Length - 1) {
      _HolderIndex = 0;
    }

    _CurrentHolder = currentHolder[_HolderIndex];
  }

  private IEnumerator ResetCamOverTime (float length) {
    Vector3 resultPos = Globals._Player._MovementController._RotationBeforeIdle * (Vector3.back * (_OrbitRadius - (_HolderIndex + 1.6f)));

    float oldRot = _LookAtRotationSpeed;
    _LookAtRotationSpeed = 10;

    _Apply = false;
    float t = 0;
    while (t <= length) {
      Vector3 endPos = resultPos + _PlayerPosition.position;
      endPos.y = _GroundOffset;

      transform.position = Vector3.Lerp (transform.position, endPos, t / length);

      t += Time.deltaTime;
      yield return null;
    }
    _Apply = true;
    _LookAtRotationSpeed = oldRot;

    _ResetCooldownTimer = _CameraResetCooldown;

    yield return null;
  }
}
