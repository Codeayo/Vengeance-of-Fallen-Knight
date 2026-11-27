using System.Linq; using System.Text; using UnityEngine; using UnityEditor;
public static class NinjaInfo {
  public static string Run(){
    var sb=new StringBuilder();
    foreach(var p in new[]{"Assets/Enemies/Prefabs/YellowNinja.prefab","Assets/Enemies/Prefabs/PatrolSkeleton.prefab","Assets/Players/Player.prefab"}){
      var go=AssetDatabase.LoadAssetAtPath<GameObject>(p); sb.AppendLine(p+" scale="+go.transform.localScale+" layer="+LayerMask.LayerToName(go.layer)+" comps="+string.Join(",",go.GetComponents<Component>().Select(c=>c.GetType().Name)));
      foreach(var t in go.GetComponentsInChildren<Transform>()) if(t!=go.transform) sb.AppendLine("   child "+t.name+" lp="+t.localPosition);
      foreach(var c in go.GetComponentsInChildren<Collider2D>()) sb.AppendLine($"   col {c.GetType().Name} trig={c.isTrigger} off={c.offset} " + (c is BoxCollider2D b? "size="+b.size : c is CapsuleCollider2D cc? "cap="+cc.size : c is CircleCollider2D ci? "r="+ci.radius:""));
      var sr=go.GetComponentInChildren<SpriteRenderer>(); if(sr) sb.AppendLine($"   sprite {sr.sprite?.name} size={sr.sprite?.bounds.size} sort={sr.sortingLayerName}");
      var rb=go.GetComponent<Rigidbody2D>(); if(rb) sb.AppendLine($"   rb grav={rb.gravityScale} cd={rb.collisionDetectionMode} constr={rb.constraints}");
      var an=go.GetComponent<Animator>(); if(an&&an.runtimeAnimatorController) sb.AppendLine("   ctrl "+AssetDatabase.GetAssetPath(an.runtimeAnimatorController)+" params="+string.Join(",",(an.runtimeAnimatorController as UnityEditor.Animations.AnimatorController).parameters.Select(x=>x.name+":"+x.type)));
    }
    var n=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/YellowNinja.prefab").GetComponent<YellowNinja>(); sb.AppendLine($"ninja walk={n.walkSpeed} chase={n.chaseSpeed} det={n.detectionRadius} atk={n.attackRadius} cd={n.attackCooldown} dmg={n.attackDamage} hp={n.maxHealth} stop={n.stopDistance} gcd={n.groundCheckDistance}");
    return sb.ToString();
  }
}
