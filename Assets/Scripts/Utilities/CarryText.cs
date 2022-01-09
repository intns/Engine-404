using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CarryText : MonoBehaviour {
  [Header ("Settings")]
  [SerializeField] private float _HeightOffset = 5;
  [SerializeField] private TextMeshPro _TMP = null;
  [HideInInspector] public Transform _FollowTarget = null;
  [HideInInspector] public Color _TextColor = Color.white;

  private void Awake () {
    _TMP.text = "";
    _TMP.color = Color.clear;
  }

  private void Update () {
    if (_FollowTarget == null) {
      return;
    }

    transform.position = _FollowTarget.position + Vector3.up * _HeightOffset;
  }

  public void SetText (int amount, int max) {
    _TMP.text = $"{amount} / {max}";
  }

  public void HandleColor (List<PikminAI> clients) {
    int red = 0;
    int ylw = 0;
    int blu = 0;

    foreach (PikminAI c in clients) {
      switch (c._Data._PikminColour) {
        case PikminColour.Red:
          red++;
          break;
        case PikminColour.Yellow:
          ylw++;
          break;
        case PikminColour.Blue:
          blu++;
          break;

        case PikminColour.Size:
        default:
          break;
      }
    }

    if (red > ylw && red > blu) {
      _TextColor = Color.red;
    }
    else if (blu > red && blu > ylw) {
      _TextColor = Color.blue;
    }
    else if (ylw > red && ylw > blu) {
      _TextColor = Color.yellow;
    }
    else {
      _TextColor = Color.red;
    }
  }

  public void FadeOut (float t = 1) {
    StartCoroutine (Fade_Coroutine (t, false));
  }

  public void FadeIn (float t = 1) {
    StartCoroutine (Fade_Coroutine (t, true));
  }

  private IEnumerator Fade_Coroutine (float t, bool fadeIn) {
    float time = 0;
    Color fromCol = fadeIn ? Color.clear : _TextColor;
    Color toCol = fadeIn ? _TextColor : Color.clear;
    while (time <= t) {
      _TMP.color = Color.Lerp (fromCol, toCol, time / t);
      time += Time.deltaTime;
      yield return null;
    }

    yield return null;
  }
}
