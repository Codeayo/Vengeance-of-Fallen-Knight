using System.Collections.Generic; using System.Linq; using System.Text; using UnityEngine; using UnityEditor.SceneManagement; using UnityEngine.Tilemaps;
public static class Islands {
  public static string Run(string scene, string tmName){
    EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
    var tm=GameObject.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).First(t=>t.name==tmName); tm.CompressBounds(); var b=tm.cellBounds; var sb=new StringBuilder();
    sb.AppendLine($"{scene} {tmName} cells {b} origin {tm.transform.position} anchor {tm.tileAnchor} cellSize {tm.layoutGrid.cellSize}");
    // per column: list of (y:tile) top->bottom
    var cols=new SortedDictionary<int,string>();
    for(int x=b.xMin;x<b.xMax;x++){ var parts=new List<string>(); for(int y=b.yMax-1;y>=b.yMin;y--){ var t=tm.GetTile(new Vector3Int(x,y,0)); if(t!=null) parts.Add(y+":"+t.name);} if(parts.Count>0) cols[x]=string.Join(" ",parts);} 
    string last=null; int runStart=0; foreach(var kv in cols){ if(kv.Value!=last){ if(last!=null) sb.AppendLine($"  x{runStart}..{kv.Key-1}: {last}"); last=kv.Value; runStart=kv.Key;} } sb.AppendLine($"  x{runStart}..: {last}");
    // sprite sizes for used tiles
    var used=new HashSet<TileBase>(); for(int x=b.xMin;x<b.xMax;x++)for(int y=b.yMin;y<b.yMax;y++){var t=tm.GetTile(new Vector3Int(x,y,0)); if(t) used.Add(t);} 
    foreach(var t in used){ var tl=t as Tile; sb.AppendLine($"  tile {t.name} sprite={(tl&&tl.sprite?tl.sprite.name+" "+tl.sprite.bounds.size:"?")} col={(tl?tl.colliderType.ToString():"?")} path={UnityEditor.AssetDatabase.GetAssetPath(t)}"); }
    return sb.ToString();
  }
}
