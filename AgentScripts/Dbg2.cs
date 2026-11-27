using System.Text; using System.Threading.Tasks; using UnityEngine;
public static class Dbg2 {
  public static async Task<string> Run(){
    var sb=new StringBuilder(); var a=GameObject.Find("Throne Guard"); var rb=a.GetComponent<Rigidbody2D>();
    for(int i=0;i<3;i++){ var cs=new ContactPoint2D[8]; int n=rb.GetContacts(cs); string c=""; for(int k=0;k<n;k++) c+=cs[k].collider.name+"/"+cs[k].otherCollider.name+"@"+cs[k].normal+" ";
      sb.AppendLine($"t={Time.time:F2} pos={rb.position} vel={rb.linearVelocity} awake={rb.IsAwake()} contacts={n}: {c} constraints={rb.constraints} mass={rb.mass} frame={Time.frameCount} fixed={Time.fixedTime:F2}"); await Task.Delay(700);} 
    return sb.ToString();
  }
}
