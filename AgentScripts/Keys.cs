using System.Text; using System.Threading.Tasks; using UnityEngine; using UnityEngine.EventSystems; using UnityEngine.InputSystem; using UnityEngine.InputSystem.LowLevel;
public static class Keys {
  static async Task Press(Key k){ var kb=Keyboard.current; InputSystem.QueueStateEvent(kb,new KeyboardState(k)); await Task.Delay(150); InputSystem.QueueStateEvent(kb,new KeyboardState()); await Task.Delay(300); }
  static string Sel(){ if(EventSystem.current==null) return "noES("+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+")"; var s=EventSystem.current.currentSelectedGameObject; return s? s.name:"none"; }
  public static async Task<string> Nav(){
    var sb=new StringBuilder(); if(Keyboard.current==null) return "no keyboard device";
    await Task.Delay(2500);
    sb.Append("start="+Sel()+" ");
    await Press(Key.DownArrow); sb.Append("down="+Sel()+" ");
    await Press(Key.DownArrow); sb.Append("down="+Sel()+" ");
    await Press(Key.Enter); await Task.Delay(400); sb.Append("enter->"+(GameObject.Find("Menu Canvas/Settings Panel")!=null?"Settings open":"no panel")+" sel="+Sel()+" ");
    var ms=GameObject.Find("Menu Canvas/Settings Panel/Music Slider")?.GetComponent<UnityEngine.UI.Slider>(); float v0=ms?ms.value:-1;
    await Press(Key.RightArrow); sb.Append($"right: music {v0:F2}->{(ms?ms.value:-1):F2} ");
    await Press(Key.LeftArrow);
    await Press(Key.Escape); await Task.Delay(400); sb.Append("esc->"+(GameObject.Find("Menu Canvas/Settings Panel")==null?"back to main":"still open")+" sel="+Sel());
    return sb.ToString();
  }
}
