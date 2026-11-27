using System.Text; using System.Threading.Tasks; using UnityEngine;
public static class Dbg {
  public static async Task<string> Skel(string name, float offset){
    var sb=new StringBuilder(); var p=GameObject.FindGameObjectWithTag("Player"); var prb=p.GetComponent<Rigidbody2D>();
    var s=GameObject.Find("Enemies/"+name); if(s==null) return "no "+name;
    var hit=Physics2D.Raycast(new Vector2(s.transform.position.x+offset,60),Vector2.down,120,LayerMask.GetMask("Ground"));
    prb.position=new Vector2(s.transform.position.x+offset, hit.point.y+1.8f); prb.linearVelocity=Vector2.zero;
    for(int i=0;i<12;i++){ await Task.Delay(400);
      if(s==null){sb.Append("enemy gone");break;}
      var pl=p.GetComponent<Player>();
      sb.Append($"t{i*0.25f:F2} d={Vector2.Distance(s.transform.position,p.transform.position):F2} sx={s.transform.position.x:F1},{s.transform.position.y:F1} px={p.transform.position.x:F1},{p.transform.position.y:F1} hp={pl.CurrentHealth} vel={s.GetComponent<Rigidbody2D>().linearVelocity.x:F1}\n");
    }
    return sb.ToString();
  }
}
