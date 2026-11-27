using System.Linq; using UnityEngine; using UnityEditor; using UnityEngine.Tilemaps;
public static class FixTiles {
  public static string Run(){
    var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Graveyard/Tiles.png").OfType<Sprite>().ToDictionary(s=>s.name);
    int fixedN=0, missing=0;
    foreach(var g in AssetDatabase.FindAssets("t:Tile",new[]{"Assets/Art/Graveyard/Tile map"})){
      var t=AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(g)); if(t==null||t.sprite!=null) continue;
      if(sprites.TryGetValue(t.name,out var s)){ t.sprite=s; EditorUtility.SetDirty(t); fixedN++; } else missing++;
    }
    AssetDatabase.SaveAssets(); return $"repaired {fixedN}, no match {missing}";
  }
}
