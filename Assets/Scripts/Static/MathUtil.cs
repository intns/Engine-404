/*
 * MathUtils.cs
 * Created by: Ambrosia
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using UnityEngine;

public static class MathUtil {
  public const float M_TAU = Mathf.PI * 2;

  /// <summary>
  /// Calculates the position of an angle on a Unit circle
  /// </summary>
  /// <param name="angle">The angle on the circle in radians</param>
  /// <returns>-1 to 1, -1 to 1</returns>
  public static Vector2 PositionInUnit (float angle) {
    return new Vector2 (Mathf.Cos (angle), Mathf.Sin (angle));
  }

  /// <summary>
  /// Calculates the position of an angle on a Unit circle
  /// </summary>
  /// <param name="angle">The angle on the circle in radians</param>
  /// <param name="radius">The radius of the circle</param>
  /// <returns>-1 to 1, -1 to 1</returns>
  public static Vector2 PositionInUnit (float angle, float radius) {
    return new Vector2 (Mathf.Cos (angle), Mathf.Sin (angle)) * radius;
  }

  /// <summary>
  /// Calculates the position of an index on a Unit circle
  /// </summary>
  /// <param name="segments">How many corners there are</param>
  /// <param name="index">The index of the corner to calculate the position of</param>
  /// <returns>-1 to 1, -1 to 1</returns>
  public static Vector2 PositionInUnit (int segments, int index) {
    float theta = (M_TAU / segments) * index;
    return new Vector2 (Mathf.Cos (theta), Mathf.Sin (theta));
  }

  /// <summary>
  /// Calculates the position of an index on a Unit circle with added offset
  /// </summary>
  /// <param name="segments">How many corners there are</param>
  /// <param name="index">The index of the corner to calculate the position of</param>
  /// <param name="offset">How much to offset the position by</param>
  /// <returns>-1 to 1, -1 to 1</returns>
  public static Vector2 PositionInUnit (int segments, int index, float offset) {
    float theta = (M_TAU / segments) * index;
    return new Vector2 (Mathf.Cos (theta + offset), Mathf.Sin (theta + offset));
  }

  /// <summary>
  /// Converts between 2D and 3D on the X and Z axis
  /// </summary>
  /// <param name="conv">The vector to convert</param>
  /// <returns>Vector3 with X and Z set to the X and Y of the Vector2</returns>
  public static Vector3 XZToXYZ (Vector2 conv, float y = 0) {
    return new Vector3 (conv.x, y, conv.y);
  }

  /// <summary>
  /// Calculates the distance between two positions, without the SQRT, making it more efficient than Vector3.Distance
  /// </summary>
  /// <param name="first">The first position</param>
  /// <param name="second">The second position</param>
  /// <param name="useY">Whether or not to use the Y axis in the calculation</param>
  /// <returns>The squared distance between 'first' and 'second'</returns>
  public static float DistanceTo (Vector3 first, Vector3 second, bool useY = true) {
    float xD = first.x - second.x;
    float yD = useY ? first.y - second.y : 0;
    float zD = first.z - second.z;
    return xD * xD + yD * yD + zD * zD;
  }
}
