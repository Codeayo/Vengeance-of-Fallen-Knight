using System.Text; using UnityEngine;
public static class Path {
  public static string Near(float x0,float x1){
    var cc=GameObject.Find("Grid/Ground1").GetComponent<CompositeCollider2D>(); var sb=new StringBuilder();
    for(int i=0;i<cc.pathCount;i++){ var pts=new Vector2[cc.GetPathPointCount(i)]; cc.GetPath(i,pts); var off=(Vector2)cc.transform.position;
      sb.Append("path"+i+": "); foreach(var p in pts){ var w=cc.transform.TransformPoint(p); if(w.x>=x0&&w.x<=x1) sb.Append($"({w.x:F1},{w.y:F1}) "); } sb.AppendLine(); }
    return sb.ToString();
  }
}
