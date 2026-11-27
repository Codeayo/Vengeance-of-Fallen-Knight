using System.Collections.Generic; using UnityEngine; using UnityEditor;
public static class ColView {
  // Draw temporary outlines of all non-trigger colliders near a point, render, then remove them.
  public static string Shot(float cx,float cy,float half,string outPath){
    var tmp=new List<GameObject>(); var px=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/WhitePixel.png");
    foreach(var c in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None)){
      var b=c.bounds; if(Mathf.Abs(b.center.x-cx)>half*2||Mathf.Abs(b.center.y-cy)>half) continue; if(b.size.x>40) continue;
      Color col=c.isTrigger? Color.cyan: Color.green;
      void Line(Vector2 p,Vector2 s){ var g=new GameObject("__line"); g.transform.position=p; g.transform.localScale=new Vector3(s.x*25f,s.y*25f,1); var r=g.AddComponent<SpriteRenderer>(); r.sprite=px; r.color=col; r.sortingLayerName="ui"; r.sortingOrder=100; tmp.Add(g);} 
      float t=0.05f; Line(new Vector2(b.center.x,b.min.y),new Vector2(b.size.x,t)); Line(new Vector2(b.center.x,b.max.y),new Vector2(b.size.x,t)); Line(new Vector2(b.min.x,b.center.y),new Vector2(t,b.size.y)); Line(new Vector2(b.max.x,b.center.y),new Vector2(t,b.size.y));
    }
    var r2=Shot_Render(cx,cy,half,outPath); foreach(var g in tmp) Object.DestroyImmediate(g); return r2;
  }
  static string Shot_Render(float cx,float cy,float half,string outPath){
    var go=new GameObject("__shotcam"); var cam=go.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=half; cam.transform.position=new Vector3(cx,cy,-10); cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Color.black; cam.aspect=1;
    var rt=new RenderTexture(600,600,24); cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt; var tex=new Texture2D(600,600,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,600,600),0,0); tex.Apply(); System.IO.File.WriteAllBytes(outPath,tex.EncodeToPNG()); RenderTexture.active=null; cam.targetTexture=null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go); return outPath;
  }
}
