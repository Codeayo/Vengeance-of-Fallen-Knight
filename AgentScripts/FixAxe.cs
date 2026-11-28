using UnityEditor; using UnityEngine;
public static class FixAxe {
  public static string Run(){
    string path="Assets/Enemies/Prefabs/AxeSkeleton.prefab"; var root=PrefabUtility.LoadPrefabContents(path);
    var sr=root.transform.Find("enemy_spirtes").GetComponent<SpriteRenderer>();
    float colBottom=float.MaxValue; foreach(var c in root.GetComponentsInChildren<Collider2D>()) if(!c.isTrigger) colBottom=Mathf.Min(colBottom,c.bounds.min.y);
    // visible feet: lowest opaque row of the idle frame
    var s=sr.sprite; var tex=s.texture; int lowest=0; bool found=false;
    var rt=RenderTexture.GetTemporary(tex.width,tex.height); Graphics.Blit(tex,rt); var prev=RenderTexture.active; RenderTexture.active=rt; var rd=new Texture2D(tex.width,tex.height); rd.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0); rd.Apply(); RenderTexture.active=prev; RenderTexture.ReleaseTemporary(rt);
    for(int y=(int)s.rect.y; y<s.rect.yMax && !found; y++) for(int x=(int)s.rect.x;x<s.rect.xMax;x++) if(rd.GetPixel(x,y).a>0.1f){ lowest=y-(int)s.rect.y; found=true; break; }
    float feet=sr.bounds.min.y + lowest/ s.pixelsPerUnit * sr.transform.lossyScale.y;
    float shift=colBottom-feet;
    sr.transform.localPosition += new Vector3(0, shift/ root.transform.lossyScale.y, 0);
    PrefabUtility.SaveAsPrefabAsset(root,path); PrefabUtility.UnloadPrefabContents(root);
    return $"colBottom={colBottom:F2} feet={feet:F2} shifted {shift:F2}";
  }
}
