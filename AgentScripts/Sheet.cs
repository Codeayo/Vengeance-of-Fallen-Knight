using System.Linq; using System.Text; using UnityEngine; using UnityEditor; using System.IO;
public static class Sheet {
  public static string List(string folder){
    var sb=new StringBuilder();
    foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{folder})){
      var p=AssetDatabase.GUIDToAssetPath(guid); var ti=(TextureImporter)AssetImporter.GetAtPath(p); var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
      var sprites=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().ToArray();
      sb.AppendLine($"{Path.GetFileName(p)} {tex.width}x{tex.height} mode={ti.spriteImportMode} ppu={ti.spritePixelsPerUnit} filter={ti.filterMode} n={sprites.Length} first={(sprites.Length>0?sprites[0].name+" "+sprites[0].rect+" border "+sprites[0].border:"")}");
    }
    return sb.ToString();
  }
  // composite contact sheet of textures (first sprite rect of each) scaled
  public static string Contact(string folder,string outPath){
    var guids=AssetDatabase.FindAssets("t:Texture2D",new[]{folder}); int cols=6,cell=160; int rows=(guids.Length+cols-1)/cols;
    var outTex=new Texture2D(cols*cell,rows*cell,TextureFormat.RGBA32,false); var bg=Enumerable.Repeat(new Color(0.25f,0.25f,0.3f,1),cols*cell*rows*cell).ToArray(); outTex.SetPixels(bg);
    for(int i=0;i<guids.Length;i++){ var p=AssetDatabase.GUIDToAssetPath(guids[i]); var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
      var rt=RenderTexture.GetTemporary(tex.width,tex.height); Graphics.Blit(tex,rt); var prev=RenderTexture.active; RenderTexture.active=rt; var rd=new Texture2D(tex.width,tex.height,TextureFormat.RGBA32,false); rd.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0); rd.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt);
      var sprites=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s=>s.rect.x).ToArray(); Rect r= sprites.Length>0? sprites[0].rect : new Rect(0,0,tex.width,tex.height);
      float sc=Mathf.Min((cell-8)/r.width,(cell-8)/r.height); int cx=(i%cols)*cell, cy=(rows-1-i/cols)*cell;
      for(int y=0;y<cell;y++)for(int x=0;x<cell;x++){ int sx=(int)(r.x+(x-4)/sc), sy=(int)(r.y+(y-4)/sc); if(sx<r.x||sy<r.y||sx>=r.xMax||sy>=r.yMax) continue; var c=rd.GetPixel(sx,sy); if(c.a>0.05f) outTex.SetPixel(cx+x,cy+y,c);} }
    outTex.Apply(); File.WriteAllBytes(outPath,outTex.EncodeToPNG()); return outPath+" "+guids.Length;
  }
}
