using System.IO; using System.Linq; using UnityEditor; using UnityEngine;
public static class BgSheet {
  public static string Run(string outPath, string[] paths){
    int cell=300; var o=new Texture2D(cell*paths.Length, cell, TextureFormat.RGBA32,false);
    o.SetPixels(Enumerable.Repeat(new Color(1,0,1,1), o.width*o.height).ToArray()); string info="";
    for(int i=0;i<paths.Length;i++){ var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]); var rt=RenderTexture.GetTemporary(tex.width,tex.height); Graphics.Blit(tex,rt); var prev=RenderTexture.active; RenderTexture.active=rt; var rd=new Texture2D(tex.width,tex.height); rd.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0); rd.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt);
      float sc=Mathf.Min((float)cell/tex.width,(float)cell/tex.height); int transparentRows=0;
      for(int y=0;y<cell;y++)for(int x=0;x<cell;x++){ int sx=(int)(x/sc), sy=(int)(y/sc); if(sx>=tex.width||sy>=tex.height) continue; var c=rd.GetPixel(sx,sy); if(c.a>0.05f) o.SetPixel(i*cell+x,y,c);} 
      for(int y=tex.height-1;y>=0;y--){ bool any=false; for(int x=0;x<tex.width;x+=4) if(rd.GetPixel(x,y).a>0.05f){any=true;break;} if(any) break; transparentRows++; }
      info+=$"{Path.GetFileName(paths[i])} {tex.width}x{tex.height} emptyTopRows={transparentRows} | "; }
    o.Apply(); File.WriteAllBytes(outPath,o.EncodeToPNG()); return info;
  }
}
