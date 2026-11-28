using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class EnemyAudit {
  static readonly Dictionary<Texture2D,Texture2D> readable=new Dictionary<Texture2D,Texture2D>();
  static Texture2D Read(Texture2D t){ if(readable.TryGetValue(t,out var r)) return r; var rt=RenderTexture.GetTemporary(t.width,t.height); Graphics.Blit(t,rt); var prev=RenderTexture.active; RenderTexture.active=rt; r=new Texture2D(t.width,t.height); r.ReadPixels(new Rect(0,0,t.width,t.height),0,0); r.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt); readable[t]=r; return r; }
  // visible (opaque) bounds of a sprite renderer in world space
  public static Rect Visible(SpriteRenderer sr){
    var s=sr.sprite; var tex=Read(s.texture); int minY=int.MaxValue,maxY=-1;
    for(int y=(int)s.rect.y;y<s.rect.yMax;y++) for(int x=(int)s.rect.x;x<s.rect.xMax;x+=1) if(tex.GetPixel(x,y).a>0.1f){ minY=Mathf.Min(minY,y); maxY=Mathf.Max(maxY,y); break; }
    if(maxY<0) return new Rect();
    float ppu=s.pixelsPerUnit, sy=Mathf.Abs(sr.transform.lossyScale.y);
    float bottomLocal=(minY-s.rect.y-s.pivot.y)/ppu, topLocal=(maxY+1-s.rect.y-s.pivot.y)/ppu;
    float wy=sr.transform.position.y;
    return new Rect(sr.bounds.min.x, wy+bottomLocal*sy, sr.bounds.size.x, (topLocal-bottomLocal)*sy);
  }
  public static async Task<string> Run(){
    var sb=new StringBuilder(); await Task.Delay(1500);
    while(Cinematic.IsPlaying) await Task.Delay(200);
    // freeze enemies in place while measuring after a settle
    await Task.Delay(1500);
    var p=GameObject.FindGameObjectWithTag("Player"); var pv=Visible(p.GetComponent<SpriteRenderer>());
    sb.AppendLine($"[{SceneManager.GetActiveScene().name}] knight visible height {pv.height:F2}");
    int mask=LayerMask.GetMask("Ground");
    var list=Object.FindObjectsByType<EnemyCore>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(e=>e.transform.position.x);
    foreach(var e in list){
      if(!e.gameObject.activeInHierarchy) continue;
      var sr=e.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.sprite!=null && r.enabled).OrderByDescending(r=>r.bounds.size.y).First();
      var v=Visible(sr);
      var hits=Physics2D.RaycastAll(new Vector2(v.center.x, v.yMin+2.5f), Vector2.down, 30f, mask).Where(h=>!h.collider.transform.IsChildOf(e.transform)).ToArray();
      float ground= hits.Length>0? hits[0].point.y : float.NaN;
      float gap=v.yMin-ground;
      string flag=Mathf.Abs(gap)>0.25f? (gap>0?" FLOATING":" SUNK"):"";
      sb.AppendLine($"  {e.DisplayName,-38} x={e.transform.position.x,6:F1} feetGap={gap,6:F2} height={v.height:F2} ({v.height/pv.height:F2}x knight) scale={e.transform.localScale.y:F2}{flag}");
    }
    return sb.ToString();
  }
}
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