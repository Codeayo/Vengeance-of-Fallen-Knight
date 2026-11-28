using System.Threading.Tasks; using UnityEngine;
public static class CapWhen {
  public static async Task<string> Title(string path){
    var f=typeof(Cinematic).GetField("titleGroup",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    for(int i=0;i<400;i++){ var g=Cinematic.Instance!=null? f.GetValue(Cinematic.Instance) as CanvasGroup : null; if(g!=null && g.alpha>0.95f){ ScreenCapture.CaptureScreenshot(path); await Task.Delay(300); return "captured at t="+Time.time.ToString("F1"); } await Task.Delay(50);} return "title never shown";
  }
}
