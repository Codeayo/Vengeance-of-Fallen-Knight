using System.Linq; using System.Text; using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.Tilemaps;
public static class Audit {
  public static string Scenes(){
    var sb=new StringBuilder();
    foreach(var path in new[]{"Assets/Scenes/Woodcamp.unity","Assets/Scenes/Grove.unity","Assets/Scenes/Taiga.unity","Assets/Castle.unity","Assets/Scenes/ThroneRoom.unity","Assets/Scenes/Credits.unity"}){
      var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
      sb.AppendLine("##### "+path+" roots: "+string.Join(", ",s.GetRootGameObjects().Select(g=>g.name)));
      int ground=LayerMask.GetMask("Ground");
      foreach(var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)){ tm.CompressBounds(); var c=tm.GetComponent<TilemapCollider2D>(); var cc=tm.GetComponent<CompositeCollider2D>(); var r=tm.GetComponent<TilemapRenderer>();
        var b=tm.cellBounds; var w0=tm.CellToWorld(b.min); var w1=tm.CellToWorld(b.max);
        sb.AppendLine($"  TM {tm.name} L={LayerMask.LayerToName(tm.gameObject.layer)} world x {w0.x:F0}..{w1.x:F0} y {w0.y:F0}..{w1.y:F0} col={(c?(c.enabled?"on":"off")+(c.isTrigger?" trig":""):"none")} comp={(cc?cc.geometryType.ToString():"-")} sort={r?.sortingLayerName}"); }
      var p=GameObject.FindGameObjectWithTag("Player");
      if(p){ var pl=p.GetComponent<Player>(); var bc=p.GetComponent<BoxCollider2D>(); var rb=p.GetComponent<Rigidbody2D>(); sb.AppendLine($"  PLAYER {p.name} pos={p.transform.position} layer={LayerMask.LayerToName(p.layer)} gl={pl?.groundLayer.value} box={bc?.size}/{bc?.offset} mat={(bc&&bc.sharedMaterial?bc.sharedMaterial.name:"none")} prefab={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p)}"); }
      else sb.AppendLine("  NO PLAYER");
      foreach(var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)){
        var t=mb.GetType().Name;
        if(t=="PatrolSkeletonAI"||t=="Enemy_behaviour"||t=="YellowNinja"||t=="Enemy1"||t=="DeathBox"||t=="SpikeDamage"||t=="LevelExit"||t=="SceneManagement"||t=="SceneSwitcher"||t=="MusicManager"||t=="GameManager"||t=="CreditScene"||t=="SceneAutoNext"||t=="GameHUD")
          sb.AppendLine($"  {t} on {mb.name} at {mb.transform.position.x:F1},{mb.transform.position.y:F1}");
      }
      int missing=0; foreach(var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)) missing+=go.GetComponents<Component>().Count(c=>c==null);
      sb.AppendLine("  missing scripts: "+missing);
      var cm=Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>(); sb.AppendLine("  cinemachine: "+(cm?cm.name:"none"));
      // ground profile summary
      if(p){ string prof=""; float last=float.NaN; for(float x=-10;x<=260;x+=2){ var h=Physics2D.Raycast(new Vector2(x,80),Vector2.down,200,ground); float y=h?h.point.y:float.NaN; if(float.IsNaN(y)!=float.IsNaN(last) || (!float.IsNaN(y) && Mathf.Abs(y-last)>0.5f)){ prof+=$"{x}:{(float.IsNaN(y)?"GAP":y.ToString("F1"))} "; last=y;} } sb.AppendLine("  ground: "+prof); }
    }
    return sb.ToString();
  }
}
