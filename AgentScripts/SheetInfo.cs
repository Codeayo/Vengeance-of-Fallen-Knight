using System.Linq; using System.Text; using UnityEditor; using UnityEngine;
public static class SheetInfo {
  public static string Run(string dir){
    var sb=new StringBuilder();
    foreach(var g in AssetDatabase.FindAssets("t:Texture2D",new[]{dir})){
      var p=AssetDatabase.GUIDToAssetPath(g); var ti=(TextureImporter)AssetImporter.GetAtPath(p); var t=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
      var sp=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s=>s.name.Length).ThenBy(s=>s.name).ToArray(); if(sp.Length==0) continue;
      var rt=RenderTexture.GetTemporary(t.width,t.height); Graphics.Blit(t,rt); var prev=RenderTexture.active; RenderTexture.active=rt; var rd=new Texture2D(t.width,t.height); rd.ReadPixels(new Rect(0,0,t.width,t.height),0,0); rd.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt);
      var s=sp[0]; int lo=int.MaxValue,hi=-1; for(int y=(int)s.rect.y;y<s.rect.yMax;y++) for(int x=(int)s.rect.x;x<s.rect.xMax;x++) if(rd.GetPixel(x,y).a>0.1f){lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);break;}
      sb.AppendLine($"{System.IO.Path.GetFileName(p),-28} tex={t.width}x{t.height} n={sp.Length} rect={s.rect.size} pivot={s.pivot} ppu={s.pixelsPerUnit} opaqueRows={lo-(int)s.rect.y}..{hi-(int)s.rect.y} mode={ti.spriteImportMode}");
    }
    return sb.ToString();
  }
}
