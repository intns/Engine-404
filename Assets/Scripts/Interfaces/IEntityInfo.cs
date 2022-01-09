public enum EntityInfo {
  Player, // aka Navi
  Piki, // aka Pikmin
  Enemy // aka Teki
}

public interface IEntityInfo {
  public EntityInfo GetEntityInfo ();
}
