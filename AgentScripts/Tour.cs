using System.Threading.Tasks; using UnityEngine;
public static class Tour {
  public static async Task<string> Go(float x, float wait){
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var hit=Physics2D.Raycast(new Vector2(x,60),Vector2.down,120,LayerMask.GetMask("Ground"));
    rb.position=new Vector2(x,hit.point.y+1.65f); rb.linearVelocity=Vector2.zero; p.transform.position=rb.position; p.TryHeal(100);
    await Task.Delay((int)(wait*1000)); return "at "+x;
  }
}
