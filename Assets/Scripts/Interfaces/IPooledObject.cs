/* 
 * IPooledObject.cs
 * Created by: Neo
 * Created on: 9/2/2020 (dd/mm/yy)
 * Created for: Replaces the "Start" function on pooled objects so we can call a similar function every time an object's spawned.
 */

public interface IPooledObject {
  // Called when the ObjectPooler reuses a model
  void OnObjectSpawn ();
}
