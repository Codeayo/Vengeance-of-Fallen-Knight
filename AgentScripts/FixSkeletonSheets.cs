using System.Text; using UnityEditor; using UnityEditor.U2D.Sprites; using UnityEngine;
public static class FixSkeletonSheets {
  // One pixel scale for every Skeleton Warrior sheet, and every frame's pivot at its feet (bottom centre).
  public static string Run(){
    var sb=new StringBuilder(); var f=new SpriteDataProviderFactories(); f.Init();
    foreach(var g in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Enemy/SKeleton/Skeleton_Warrior"})){
      var p=AssetDatabase.GUIDToAssetPath(g); var ti=(TextureImporter)AssetImporter.GetAtPath(p);
      var dp=f.GetSpriteEditorDataProviderFromObject(ti); dp.InitSpriteEditorDataProvider();
      var rects=dp.GetSpriteRects(); foreach(var r in rects){ r.alignment=SpriteAlignment.BottomCenter; r.pivot=new Vector2(0.5f,0f); }
      dp.SetSpriteRects(rects); dp.Apply();
      ti.spritePixelsPerUnit=16; ti.SaveAndReimport();
      sb.Append(System.IO.Path.GetFileName(p)+" "); }
    return "fixed: "+sb;
  }
}
