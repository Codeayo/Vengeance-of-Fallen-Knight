using System.Linq; using System.Text; using UnityEngine; using UnityEngine.Tilemaps;
public static class Tiles {
  public static string Check(){
    var tm=GameObject.Find("Grid/Ground1").GetComponent<Tilemap>(); var b=tm.cellBounds; var sb=new StringBuilder();
    var stats=new System.Collections.Generic.Dictionary<string,int>(); int minNoneX=int.MaxValue;
    for(int x=b.xMin;x<b.xMax;x++) for(int y=b.yMin;y<b.yMax;y++){ var c=new Vector3Int(x,y,0); if(!tm.HasTile(c)) continue;
      var ct=tm.GetColliderType(c); var t=tm.GetTile(c); string k=(t!=null?t.GetType().Name:"null")+"/"+ct; stats[k]=stats.TryGetValue(k,out var v)?v+1:1;
      if(ct==Tile.ColliderType.None) minNoneX=Mathf.Min(minNoneX,x);
      if(x==tm.WorldToCell(new Vector3(100,0,0)).x && sb.Length<600) sb.Append($"[{y}:{t?.name}/{ct}] ");
    }
    foreach(var kv in stats) sb.AppendLine(kv.Key+"="+kv.Value);
    sb.AppendLine("first None col cell x="+minNoneX+" world="+tm.CellToWorld(new Vector3Int(minNoneX,0,0)).x);
    var cc=tm.GetComponent<CompositeCollider2D>(); sb.AppendLine("composite bounds "+cc.bounds+" paths "+cc.pathCount);
    for(int i=0;i<cc.pathCount;i++) sb.AppendLine("path"+i+" pts="+cc.GetPathPointCount(i));
    return sb.ToString();
  }
}
