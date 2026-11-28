using System.IO; using System.Linq; using UnityEditor; using UnityEngine;
public static class SkelPreview {
  public static string Run(){
    string dir="Assets/Enemy/SKeleton/Skeleton_Warrior/"; string[] sheets={"Idle","Walk","Run","Attack_1","Attack_2","Attack_3","Protect","Hurt","Dead"};
    int cell=220, scale=3; var o=new Texture2D(cell*sheets.Length, cell, TextureFormat.RGBA32,false);
    o.SetPixels(Enumerable.Repeat(new Color(0.18f,0.18f,0.22f,1),o.width*o.height).ToArray());
    for(int i=0;i<sheets.Length;i++){
      var p=dir+sheets[i]+".png"; var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
      var s=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(x=>x.name.Length).ThenBy(x=>x.name).ElementAt(sheets[i]=="Dead"?3:0);
      var rt=RenderTexture.GetTemporary(tex.width,tex.height); Graphics.Blit(tex,rt); var prev=RenderTexture.active; RenderTexture.active=rt; var rd=new Texture2D(tex.width,tex.height); rd.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0); rd.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt);
      int ox=i*cell+(cell-(int)s.rect.width*scale)/2, oy=10;
      for(int y=0;y<s.rect.height;y++)for(int x=0;x<s.rect.width;x++){ var c=rd.GetPixel((int)s.rect.x+x,(int)s.rect.y+y); if(c.a<0.1f) continue; for(int a=0;a<scale;a++)for(int b=0;b<scale;b++){int px=ox+x*scale+a, py=oy+y*scale+b; if(px>=i*cell&&px<(i+1)*cell&&py<cell) o.SetPixel(px,py,c);} }
    }
    o.Apply(); File.WriteAllBytes("Screenshots/skeleton_warrior_sprites.png",o.EncodeToPNG()); return "ok";
  }
}
