using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
public static class Shot {
  // renders world rect (cx,cy,halfHeight) at w x h
  public static string Render(float cx, float cy, float halfH, int w, int h, string outPath){
    var go=new GameObject("__shotcam"); var cam=go.AddComponent<Camera>();
    cam.orthographic=true; cam.orthographicSize=halfH; cam.transform.position=new Vector3(cx,cy,-10);
    cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Color.black; cam.aspect=(float)w/h;
    var main=Camera.main; if(main!=null){ cam.cullingMask=main.cullingMask & ~(1<<5);} 
    var rt=new RenderTexture(w,h,24); cam.targetTexture=rt; cam.Render();
    RenderTexture.active=rt; var tex=new Texture2D(w,h,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,w,h),0,0); tex.Apply();
    File.WriteAllBytes(outPath, tex.EncodeToPNG()); RenderTexture.active=null; cam.targetTexture=null;
    Object.DestroyImmediate(rt); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    return outPath;
  }
}
public static class Overview {
  public static string Run(string scenePath, string tag, float x0, float x1, float cy, float half){
    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    float w=x1-x0; int pw=1800, ph=(int)(pw*(half*2)/w);
    return Shot.Render((x0+x1)/2f, cy, half, pw, ph, "AgentScripts/shots/ov_"+tag+".png");
  }
}