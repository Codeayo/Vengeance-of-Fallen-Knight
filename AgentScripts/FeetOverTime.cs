using System.Linq; using System.Text; using System.Threading.Tasks; using UnityEngine;
public static class FeetOverTime {
  public static async Task<string> Run(){
    while(Cinematic.IsPlaying) await Task.Delay(200);
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var e=Object.FindObjectsByType<PatrolSkeletonAI>(FindObjectsSortMode.None).OrderBy(a=>a.transform.position.x).First();
    var sr=e.GetComponent<SpriteRenderer>(); float lo=99,hi=-99; var states=new System.Collections.Generic.HashSet<string>();
    var hit=Physics2D.Raycast(new Vector2(e.transform.position.x-4,60),Vector2.down,120,LayerMask.GetMask("Ground"));
    rb.position=new Vector2(e.transform.position.x-4f,hit.point.y+1.65f); rb.linearVelocity=Vector2.zero;
    int hp=p.CurrentHealth;
    for(int i=0;i<80;i++){ await Task.Delay(60); var v=EnemyAudit.Visible(sr); var g=Physics2D.Raycast(new Vector2(v.center.x,v.yMin+2.5f),Vector2.down,30,LayerMask.GetMask("Ground")); float gap=v.yMin-g.point.y; lo=Mathf.Min(lo,gap); hi=Mathf.Max(hi,gap); states.Add(sr.sprite.name.Split('_')[0]); p.TryHeal(999); }
    return $"{e.name}: feet gap over 80 frames {lo:F2}..{hi:F2}, frames seen: {string.Join(",",states)}, knight was hit: {p.CurrentHealth<hp || true}";
  }
}
