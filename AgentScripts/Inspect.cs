using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
public static class Inspect {
  static void Walk(Transform t, int d, StringBuilder sb, int maxD){
    if(d>maxD) return;
    var comps = string.Join(",", t.GetComponents<Component>().Where(c=>c!=null && !(c is Transform)).Select(c=>c.GetType().Name));
    int missing = t.GetComponents<Component>().Count(c=>c==null);
    sb.AppendLine(new string(' ',d*2)+t.name+(t.gameObject.activeSelf?"":" [off]")+" L="+LayerMask.LayerToName(t.gameObject.layer)+" p="+t.position.ToString("F1")+" ["+comps+"]"+(missing>0?" MISSING:"+missing:""));
    foreach(Transform c in t) Walk(c,d+1,sb,maxD);
  }
  public static string Scene(string path, int maxD){
    var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    var sb=new StringBuilder();
    foreach(var r in s.GetRootGameObjects()) Walk(r.transform,0,sb,maxD);
    foreach(var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)){
      tm.CompressBounds();
      var col=tm.GetComponent<TilemapCollider2D>();
      sb.AppendLine("TM "+tm.name+" bounds="+tm.cellBounds+" tiles="+tm.GetUsedTilesCount()+" col="+(col!=null?(col.enabled+" trig="+col.isTrigger):"none")+" sort="+tm.GetComponent<TilemapRenderer>()?.sortingLayerName+"/"+tm.GetComponent<TilemapRenderer>()?.sortingOrder);
    }
    return sb.ToString();
  }
  public static string Layers(){
    var sb=new StringBuilder();
    for(int i=0;i<32;i++){var n=LayerMask.LayerToName(i); if(n!="") sb.Append(i+":"+n+" ");}
    sb.AppendLine(); sb.AppendLine("Sorting: "+string.Join(",",SortingLayer.layers.Select(l=>l.name)));
    sb.AppendLine("Gravity "+Physics2D.gravity);
    return sb.ToString();
  }
}
