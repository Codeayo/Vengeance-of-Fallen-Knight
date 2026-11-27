using System.Linq; using System.Text; using UnityEngine; using UnityEditor; using UnityEngine.Tilemaps;
public static class Props {
  public static string Run(){
    var sb=new StringBuilder();
    foreach(var p in new[]{"Assets/Art/Graveyard/Grave1.prefab","Assets/Art/Graveyard/grave2.prefab","Assets/Prefab/Coin.prefab"}){
      var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);
      foreach(var r in go.GetComponentsInChildren<SpriteRenderer>(true)) sb.AppendLine($"{p} {r.name} sprite={r.sprite?.name} size={r.bounds.size} scale={go.transform.localScale} comps={string.Join(",",go.GetComponents<Component>().Select(c=>c.GetType().Name))}");
    }
    var sp=GameObject.Find("Grid/Spikes").GetComponent<Tilemap>(); sp.CompressBounds(); var b=sp.cellBounds;
    for(int x=b.xMin;x<b.xMax;x++)for(int y=b.yMin;y<b.yMax;y++){var c=new Vector3Int(x,y,0); if(sp.HasTile(c)) sb.Append($"[{sp.GetCellCenterWorld(c).x:F0},{sp.GetCellCenterWorld(c).y:F1} {sp.GetSprite(c)?.name} {sp.GetSprite(c)?.bounds.size}] ");}
    sb.AppendLine();
    var heart=GameObject.Find("Canvas/Health_UI/HealthSlider/Heart_Icon"); sb.AppendLine("heart "+(heart?heart.GetComponent<UnityEngine.UI.Image>().sprite?.name:"none"));
    var coin=GameObject.Find("Canvas/Coin UI/Coin_Icon"); sb.AppendLine("coin "+(coin?coin.GetComponent<UnityEngine.UI.Image>().sprite?.name:"none"));
    var pc=GameObject.FindGameObjectWithTag("Player").GetComponent<Player_Collectables>(); var so=new SerializedObject(pc); sb.AppendLine("flask "+so.FindProperty("healingFlaskSprite").objectReferenceValue?.name);
    var st=GameObject.Find("Grid/Statues/Salt_0"); sb.AppendLine("statue scale "+st.transform.localScale+" bounds "+st.GetComponent<SpriteRenderer>().bounds.size+" sort "+st.GetComponent<SpriteRenderer>().sortingLayerName);
    foreach(var n in new[]{"Grid/Clumns","Grid/Prop","Grid/Grill_tile","Grid/Bushes","Grid/Statues","Grid/Backtiles"}){var r=GameObject.Find(n).GetComponent<TilemapRenderer>(); sb.AppendLine(n+" sort "+r.sortingLayerName+"/"+r.sortingOrder);}
    return sb.ToString();
  }
}
