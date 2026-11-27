using System.Linq; using System.Text; using UnityEngine; using UnityEditor;
public static class Info {
  public static string Run(){
    var sb=new StringBuilder();
    foreach(var p in new[]{"Assets/Art/Graveyard/Background_0.png","Assets/Art/Graveyard/Background_1.png","Assets/Art/Graveyard/Salt.png","Assets/Art/Graveyard/brush.png","Assets/Art/Graveyard/Grass_background_1.png","Assets/Art/Graveyard/Grass_background_2.png","Assets/Art/Graveyard/Social/moon.png"}){
      var sp=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().ToArray(); var ti=(TextureImporter)AssetImporter.GetAtPath(p);
      sb.AppendLine(p+" sprites="+string.Join(";",sp.Select(s=>s.name+" "+s.rect.size+" ppu"+s.pixelsPerUnit+" b="+s.bounds.size)));
    }
    foreach(var g in AssetDatabase.FindAssets("t:TMP_FontAsset")) sb.AppendLine("font "+AssetDatabase.GUIDToAssetPath(g));
    foreach(var g in AssetDatabase.FindAssets("t:Material", new[]{"Assets/TextMesh Pro"})) sb.AppendLine("mat "+AssetDatabase.GUIDToAssetPath(g));
    var bg1=GameObject.Find("BackGrounds/BG1"); if(bg1){var r=bg1.GetComponent<SpriteRenderer>(); sb.AppendLine("BG1 sprite "+r.sprite.name+" "+AssetDatabase.GetAssetPath(r.sprite)+" size "+r.bounds.size+" mat "+r.sharedMaterial.name+" sort "+r.sortingLayerName+"/"+r.sortingOrder);}
    var bg2=GameObject.Find("BackGrounds/BG2"); if(bg2){var r=bg2.GetComponent<SpriteRenderer>(); sb.AppendLine("BG2 sprite "+r.sprite.name+" "+AssetDatabase.GetAssetPath(r.sprite)+" size "+r.bounds.size+" sort "+r.sortingLayerName+"/"+r.sortingOrder);}
    var pl=GameObject.FindGameObjectWithTag("Player"); var psr=pl.GetComponent<SpriteRenderer>(); var an=pl.GetComponent<Animator>();
    sb.AppendLine("player sprite "+psr.sprite.name+" "+AssetDatabase.GetAssetPath(psr.sprite)+" bounds "+psr.bounds+" mat "+psr.sharedMaterial.name+" sort "+psr.sortingLayerName+"/"+psr.sortingOrder+" ctrl "+AssetDatabase.GetAssetPath(an.runtimeAnimatorController));
    var mm=Object.FindFirstObjectByType<MusicManager>(); sb.AppendLine("music menu="+(mm.menuTrack?mm.menuTrack.name:"null")+" grave="+(mm.graveyardTrack?mm.graveyardTrack.name:"null")+" source="+(mm.musicSource!=null));
    var tm=GameObject.Find("Grid/Ground1").GetComponent<UnityEngine.Tilemaps.TilemapRenderer>(); sb.AppendLine("ground mat "+tm.sharedMaterial.name);
    var cm=Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>(); sb.AppendLine("cm lens ortho "+cm.Lens.OrthographicSize+" follow "+(cm.Follow?cm.Follow.name:"null"));
    var r2=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline; sb.AppendLine("rp "+(r2?r2.name:"null"));
    foreach(var e in new[]{"Ememy/Enemy","Ememy/PatrolSkeleton"}){var go=GameObject.Find(e); sb.AppendLine(e+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go));}
    return sb.ToString();
  }
}
