using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
public static class Probe {
  public static string Ground(){
    var sb=new StringBuilder();
    var g=GameObject.Find("Grid/Ground1");
    var tm=g.GetComponent<Tilemap>();
    var rb=g.GetComponent<Rigidbody2D>(); var cc=g.GetComponent<CompositeCollider2D>(); var tc=g.GetComponent<TilemapCollider2D>();
    sb.AppendLine($"rb {rb.bodyType} comp geom={cc.geometryType} gen={cc.generationType} paths={cc.pathCount} tc.composite={tc.compositeOperation} cellSize={g.GetComponentInParent<Grid>().cellSize} scale={g.transform.lossyScale}");
    var b=tm.cellBounds; int cells=0;
    // top surface world y per world x (sampled each cell column)
    string line=""; int lastTop=int.MinValue;
    for(int x=b.xMin;x<b.xMax;x++){
      int top=int.MinValue;
      for(int y=b.yMax-1;y>=b.yMin;y--){ if(tm.HasTile(new Vector3Int(x,y,0))){cells++; if(top==int.MinValue) top=y;} }
      var w=tm.CellToWorld(new Vector3Int(x,top==int.MinValue?0:top+1,0));
      string s= top==int.MinValue? $"{w.x:F0}:GAP" : $"{w.x:F0}:{w.y:F1}";
      if(top!=lastTop){ line+=s+" "; lastTop=top;}
    }
    sb.AppendLine("cells="+cells); sb.AppendLine(line);
    var p=GameObject.Find("Player");
    var pb=p.GetComponent<BoxCollider2D>(); var prb=p.GetComponent<Rigidbody2D>(); var pl=p.GetComponent<Player>();
    sb.AppendLine($"player box size={pb.size} off={pb.offset} trig={pb.isTrigger} bounds={pb.bounds} rb={prb.bodyType} grav={prb.gravityScale} cd={prb.collisionDetectionMode} interp={prb.interpolation} constr={prb.constraints} mat={(pb.sharedMaterial?pb.sharedMaterial.name:"none")} groundLayer={pl.groundLayer.value} enemyLayer={pl.enemyLayer.value} r={pl.groundCheckRadius} tag={p.tag} scale={p.transform.localScale}");
    var sp=GameObject.Find("Grid/Spikes"); var sbx=sp.GetComponent<BoxCollider2D>(); sb.AppendLine($"spikes box {sbx.bounds} trig={sbx.isTrigger}");
    for(int i=0;i<32;i++){ var n=LayerMask.LayerToName(i); if(n=="")continue; string row=n+": "; for(int j=0;j<32;j++){var m=LayerMask.LayerToName(j); if(m!="" && Physics2D.GetIgnoreLayerCollision(i,j)) row+="!"+m+" ";} sb.AppendLine(row);}
    var conf=GameObject.Find("Camera/confiner").GetComponent<PolygonCollider2D>(); sb.AppendLine("confiner bounds "+conf.bounds+" trig="+conf.isTrigger);
    foreach(var e in new[]{"Ememy/Enemy","Ememy/PatrolSkeleton"}){var go=GameObject.Find(e); var r=go.GetComponent<Rigidbody2D>(); sb.AppendLine(e+" rb="+r.bodyType+" grav="+r.gravityScale+" layer="+LayerMask.LayerToName(go.layer));}
    return sb.ToString();
  }
}
