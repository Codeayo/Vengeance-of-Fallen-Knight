using System.Collections; using System.Reflection; using System.Text; using System.Threading.Tasks; using UnityEngine;
public static class Dbg3 {
  public static async Task<string> Run(){
    var sb=new StringBuilder(); var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var g=GameObject.Find("Throne Guard"); if(g==null) return "none";
    var ai=g.GetComponent<PatrolSkeletonAI>(); var m=typeof(Player).GetMethod("DoDamage",BindingFlags.NonPublic|BindingFlags.Instance);
    var rb=p.GetComponent<Rigidbody2D>(); rb.position=new Vector2(g.transform.position.x-3.2f, g.transform.position.y-0.6f); await Task.Delay(400);
    for(int i=0;i<6;i++){ p.GetComponent<SpriteRenderer>().flipX = g.transform.position.x < p.transform.position.x; await Task.Delay(50);
      var hits=Physics2D.OverlapCircleAll(p.attackPoint.position,p.attackRange,p.enemyLayer); string hs=""; foreach(var h in hits) hs+=h.name+"("+LayerMask.LayerToName(h.gameObject.layer)+") ";
      p.StartCoroutine((IEnumerator)m.Invoke(p,null)); p.TryHeal(999); await Task.Delay(500);
      sb.AppendLine($"swing {i}: guard hp={ai.HealthNormalized:F2} gx={g.transform.position.x:F1} px={p.transform.position.x:F1} ap={p.attackPoint.position.x:F1} mask={p.enemyLayer.value} hits=[{hs}] dmg={p.AttackDamage}"); }
    return sb.ToString();
  }
}
