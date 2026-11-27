using System.Linq; using System.Text; using UnityEngine; using UnityEditor; using System.IO;
public static class Sheets2 {
  // Contact sheet of every sprite inside one texture, labelled by index.
  public static string Sprites(string texPath, string outPath, int cell){
    var sprites=AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().OrderBy(s=>{int n; var p=s.name.Split('_'); return int.TryParse(p.Last(),out n)?n:0;}).ToArray();
    var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
    var rt=RenderTexture.GetTemporary(tex.width,tex.height); Graphics.Blit(tex,rt); var prev=RenderTexture.active; RenderTexture.active=rt; var rd=new Texture2D(tex.width,tex.height,TextureFormat.RGBA32,false); rd.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0); rd.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt);
    int cols=12, rows=(sprites.Length+cols-1)/cols; var o=new Texture2D(cols*cell,rows*cell,TextureFormat.RGBA32,false); o.SetPixels(Enumerable.Repeat(new Color(0.22f,0.22f,0.26f),o.width*o.height).ToArray());
    for(int i=0;i<sprites.Length;i++){ var r=sprites[i].rect; float sc=Mathf.Min((cell-6)/r.width,(cell-6)/r.height); int cx=(i%cols)*cell, cy=(rows-1-i/cols)*cell;
      for(int y=0;y<cell;y++)for(int x=0;x<cell;x++){int sx=(int)(r.x+(x-3)/sc), sy=(int)(r.y+(y-3)/sc); if(sx<r.x||sy<r.y||sx>=r.xMax||sy>=r.yMax) continue; var c=rd.GetPixel(sx,sy); if(c.a>0.1f) o.SetPixel(cx+x,cy+y,c);} 
      // index tick marks: draw i in binary as dots at top
      for(int b=0;b<8;b++) if(((i>>b)&1)==1) for(int dx=0;dx<3;dx++)for(int dy=0;dy<3;dy++) o.SetPixel(cx+cell-4-b*4+dx, cy+cell-4+dy-0, Color.red);
    }
    o.Apply(); File.WriteAllBytes(outPath,o.EncodeToPNG()); return outPath+" n="+sprites.Length+" names: "+string.Join(",",sprites.Take(60).Select((s,i)=>i+":"+s.name));
  }
  public static string Packs(){
    var sb=new StringBuilder();
    foreach(var d in new[]{"Assets/Art/Castle-Hall","Assets/Art/Dungeon-Prison","Assets/Art/Fantasy Swamp Forest","Assets/Art/Final Taiga Forest","Assets/Art/Mushroom_Grove","Assets/Art/Wood-Camp"}){
      sb.AppendLine("## "+d);
      foreach(var g in AssetDatabase.FindAssets("t:Texture2D",new[]{d})){ var p=AssetDatabase.GUIDToAssetPath(g); var t=AssetDatabase.LoadAssetAtPath<Texture2D>(p); int n=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().Count(); sb.AppendLine($"  {p.Substring(d.Length+1)} {t.width}x{t.height} sprites={n}"); }
      sb.AppendLine("  tiles="+AssetDatabase.FindAssets("t:TileBase",new[]{d}).Length+" prefabs="+AssetDatabase.FindAssets("t:Prefab",new[]{d}).Length);
    }
    return sb.ToString();
  }
}
