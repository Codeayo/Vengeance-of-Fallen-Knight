using System.Threading.Tasks; using UnityEngine; using UnityEngine.SceneManagement; using TMPro;
public static class CreditsTest {
  public static async Task<string> Run(){
    var t=Object.FindFirstObjectByType<CreditScene>(); var txt=t.GetComponentInChildren<TextMeshProUGUI>();
    bool ok=txt.font.HasCharacters(txt.GetParsedText(), out var missing, true, true);
    string miss = missing==null? "" : string.Join(",", System.Array.ConvertAll(missing, c=>((int)c).ToString("X")));
    await Task.Delay(2500); ScreenCapture.CaptureScreenshot("AgentScripts/shots/credits_1.png");
    var rt=txt.rectTransform; rt.anchoredPosition+=new Vector2(0,2600); await Task.Delay(400); ScreenCapture.CaptureScreenshot("AgentScripts/shots/credits_2.png");
    rt.anchoredPosition+=new Vector2(0,2000); await Task.Delay(400); ScreenCapture.CaptureScreenshot("AgentScripts/shots/credits_3.png");
    t.scrollSpeed=3000; for(int i=0;i<100 && SceneManager.GetActiveScene().name=="Credits";i++){ await Task.Delay(100); if(i==35) ScreenCapture.CaptureScreenshot("AgentScripts/shots/credits_end.png"); }
    for(int i=0;i<40 && SceneManager.GetActiveScene().name=="Credits";i++) await Task.Delay(250);
    return $"glyphs ok={ok} missing=[{miss}] scene now={SceneManager.GetActiveScene().name} save cleared={!GameSave.HasSavedGame}";
  }
}
