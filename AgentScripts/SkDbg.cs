using System.Collections; using System.Reflection; using System.Text; using System.Threading.Tasks; using UnityEngine;
public static class SkDbg {
  public static async Task<string> Run(){
    var sb=new StringBuilder(); while(Cinematic.IsPlaying) await Task.Delay(200);
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var s=GameObject.Find("Enemies/Restless Skeleton"); var ai=s.GetComponent<PatrolSkeletonAI>();
    var m=typeof(Player).GetMethod("DoDamage",BindingFlags.NonPublic|BindingFlags.Instance,null,System.Type.EmptyTypes,null);
    sb.AppendLine($"skel layer={LayerMask.LayerToName(s.layer)} cols={s.GetComponents<Collider2D>().Length} enabledAI={ai.enabled} hp={ai.HealthNormalized} mask={p.enemyLayer.value} core={(s.GetComponent<EnemyCore>()!=null)}");
    for(int i=0;i<6;i++){ rb.position=new Vector2(s.transform.position.x-2.2f,s.transform.position.y); rb.linearVelocity=Vector2.zero; await Task.Delay(100);
      p.GetComponent<SpriteRenderer>().flipX=false; var f=typeof(Player).GetField("attackPointFacingRightPosition",BindingFlags.NonPublic|BindingFlags.Instance); Vector3 b=(Vector3)f.GetValue(p); p.attackPoint.localPosition=new Vector3(Mathf.Abs(b.x),b.y,b.z);
      var hits=Physics2D.OverlapCircleAll(p.attackPoint.position,p.attackRange,p.enemyLayer); string hs=""; foreach(var h in hits) hs+=h.name+" ";
      p.StartCoroutine((IEnumerator)m.Invoke(p,null)); await Task.Delay(500);
      sb.AppendLine($"swing {i}: hp={ai.HealthNormalized:F2} px={p.transform.position.x:F1} sx={s.transform.position.x:F1} ap={p.attackPoint.position.x:F1} hits=[{hs}] guard={typeof(PatrolSkeletonAI).GetField("guarding",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ai)}"); }
    return sb.ToString();
  }
}
