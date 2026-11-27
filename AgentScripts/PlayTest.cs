using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public static class PlayTest {
  // Drop the player at many x positions; report where it ends up below expected ground.
  public static async Task<string> Drop(float x0, float x1, float step){
    var sb=new StringBuilder();
    var p=GameObject.FindGameObjectWithTag("Player"); if(p==null) return "no player";
    var rb=p.GetComponent<Rigidbody2D>(); var col=p.GetComponent<Collider2D>();
    int ground=LayerMask.GetMask("Ground");
    for(float x=x0;x<=x1;x+=step){
      var hit=Physics2D.Raycast(new Vector2(x,40),Vector2.down,80,ground);
      if(!hit){ sb.Append($"{x}:NOGROUND(scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}) "); break; }
      rb.position=new Vector2(x,hit.point.y+3f); rb.linearVelocity=Vector2.zero; p.transform.position=rb.position;
      await Task.Delay(700);
      float bottom=col.bounds.min.y;
      if(bottom < hit.point.y-0.3f) sb.Append($"{x}:FELL(ground {hit.point.y:F1} bottom {bottom:F1}) ");
    }
    return sb.Length==0?"all ok":sb.ToString();
  }
}
