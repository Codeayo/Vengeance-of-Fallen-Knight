using System.Threading.Tasks; using UnityEngine; using TMPro;
public static class CreditsShot { public static async Task<string> Run(){ var t=Object.FindFirstObjectByType<CreditScene>(); t.scrollSpeed=0; var rt=t.GetComponentInChildren<TextMeshProUGUI>().rectTransform; await Task.Delay(1500);
  rt.anchoredPosition+=new Vector2(0,2350); await Task.Delay(300); ScreenCapture.CaptureScreenshot("AgentScripts/shots/cr_a.png"); await Task.Delay(300);
  rt.anchoredPosition+=new Vector2(0,900); await Task.Delay(300); ScreenCapture.CaptureScreenshot("AgentScripts/shots/cr_b.png"); await Task.Delay(300);
  bool ok=t.GetComponentInChildren<TextMeshProUGUI>().font.HasCharacters(t.GetComponentInChildren<TextMeshProUGUI>().GetParsedText(), out var m, true, true); return "glyphs ok="+ok; } }
