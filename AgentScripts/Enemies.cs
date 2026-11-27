using System.Text; using UnityEngine; using UnityEditor;
public static class Enemies {
  public static string Run(){
    var sb=new StringBuilder();
    var e=GameObject.Find("Ememy/Enemy").GetComponent<Enemy_behaviour>();
    sb.AppendLine($"Enemy mask={e.raycaskMask.value} len={e.rayCastLength} atk={e.attackDistance} spd={e.moveSpeed} hp={e.maxHealth} dmg={e.attackDamage}");
    foreach(var c in e.GetComponentsInChildren<Collider2D>()) sb.AppendLine($"  {c.name} {c.GetType().Name} trig={c.isTrigger} bounds={c.bounds.size}");
    var s=GameObject.Find("Ememy/PatrolSkeleton").GetComponent<PatrolSkeletonAI>();
    sb.AppendLine($"Skel ground={s.groundLayer.value} chase={s.chaseRange} atk={s.attackRange} dmg={s.attackDamage} scale={s.transform.localScale}");
    foreach(var c in s.GetComponentsInChildren<Collider2D>()) sb.AppendLine($"  {c.name} {c.GetType().Name} trig={c.isTrigger} size={c.bounds.size} center={c.bounds.center}");
    var sr=s.GetComponent<SpriteRenderer>(); sb.AppendLine("  sprite bounds "+sr.bounds+" sort "+sr.sortingLayerName);
    var ninja=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/YellowNinja.prefab").GetComponent<YellowNinja>();
    sb.AppendLine($"Ninja ground={ninja.whatIsGround.value} player={ninja.whatIsPlayer.value} hp={ninja.maxHealth} dmg={ninja.attackDamage} scale={ninja.transform.localScale}");
    var p=GameObject.FindGameObjectWithTag("Player"); var psr=p.GetComponent<SpriteRenderer>();
    sb.AppendLine("player layer "+LayerMask.LayerToName(p.layer)+" sprite rect "+psr.sprite.rect+" pivot "+psr.sprite.pivot+" ppu "+psr.sprite.pixelsPerUnit);
    return sb.ToString();
  }
}
