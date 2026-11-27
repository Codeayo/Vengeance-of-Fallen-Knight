using System.Text; using System.Threading.Tasks; using UnityEngine; using UnityEngine.UI; using UnityEngine.EventSystems;
public static class MenuTest {
  static Button B(string path){ var g=GameObject.Find(path); return g? g.GetComponent<Button>():null; }
  static string Sel(){ var s=EventSystem.current.currentSelectedGameObject; return s? s.name:"none"; }
  public static async Task<string> Panels(){
    var sb=new StringBuilder();
    sb.Append("start sel="+Sel()+" ");
    foreach(var (btn,panel) in new[]{("Menu Canvas/Main Panel/SettingsButton","Settings Panel"),("Menu Canvas/Main Panel/LevelSelectButton","Level Select Panel"),("Menu Canvas/Main Panel/AboutButton","About Panel")}){
      var b=B(btn); b.onClick.Invoke(); await Task.Delay(600);
      var p=GameObject.Find("Menu Canvas/"+panel); sb.Append($"{panel}: active={(p!=null&&p.activeInHierarchy)} sel={Sel()} | ");
      ScreenCapture.CaptureScreenshot("AgentScripts/shots/panel_"+panel.Replace(" ","")+".png");
      await Task.Delay(500);
      // back via back button
      var back=GameObject.Find("Menu Canvas/"+panel+"/Back").GetComponent<Button>(); back.onClick.Invoke(); await Task.Delay(500);
      sb.Append($"after back main={GameObject.Find("Menu Canvas/Main Panel")!=null} sel={Sel()} || ");
    }
    return sb.ToString();
  }
}
