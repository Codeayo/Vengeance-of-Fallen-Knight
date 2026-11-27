using System.Text; using UnityEngine;
public static class Ray {
  public static string Test(){
    var sb=new StringBuilder(); int g=LayerMask.GetMask("Ground");
    var cc=GameObject.Find("Grid/Ground1").GetComponent<CompositeCollider2D>();
    for(int i=0;i<cc.pathCount;i++){ var pts=new Vector2[cc.GetPathPointCount(i)]; cc.GetPath(i,pts); float mn=1e9f,mx=-1e9f; foreach(var p in pts){mn=Mathf.Min(mn,p.x);mx=Mathf.Max(mx,p.x);} sb.AppendLine($"path{i} x {mn:F0}..{mx:F0}"); }
    foreach(float x in new[]{5f,50f,91f,95f,130f,200f,300f,400f}){
      var h=Physics2D.Raycast(new Vector2(x,40),Vector2.down,80,g);
      var all=Physics2D.RaycastAll(new Vector2(x,40),Vector2.down,80);
      string names=""; foreach(var a in all) names+=a.collider.name+"@"+a.point.y.ToString("F1")+"("+LayerMask.LayerToName(a.collider.gameObject.layer)+") ";
      sb.AppendLine($"x{x}: ground={(h?h.point.y.ToString("F1"):"none")} all={names}");
    }
    return sb.ToString();
  }
}
