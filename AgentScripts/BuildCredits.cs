using UnityEngine; using UnityEditor.SceneManagement; using TMPro;
public static class BuildCredits {
  public static string Run(){
    var s=EditorSceneManager.OpenScene("Assets/Scenes/Credits.unity", OpenSceneMode.Single);
    TMP_Text t=null; foreach(var x in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) if(x.name=="Text (TMP)") t=x;
    t.text =
      "<size=140%><color=#E8C27A>EPILOGUE</color></size>\n\n" +
      "<i>Lord Malvric lies dead at the foot of the throne he stole.\n" +
      "The eleven of the old guard are avenged, and the truth of the night\n" +
      "Sir Aldric fell is carried to every corner of the realm.\n\n" +
      "The Shard of the Crown is returned to the old king's line.\n" +
      "The fallen knight lays down his sword before the empty throne\n" +
      "and, at last, is allowed to rest.</i>\n\n\n" +
      "<size=140%><color=#E8C27A>VENGEANCE OF THE FALLEN KNIGHT</color></size>\n\n\n" +
      "<color=#E8C27A>Game Design & Development</color>\nThe Vengeance of the Fallen Knight team\n\n" +
      "<color=#E8C27A>Hero</color>\nSoulslike Knight sprites\n\n" +
      "<color=#E8C27A>Environments</color>\nGraveyard, Wood Camp, Mushroom Grove, Final Taiga Forest and Castle Hall packs by Anokolisa\n\n" +
      "<color=#E8C27A>Interface</color>\nTiny RPG Mana Soul GUI by Gabriel \"tiopalada\" Lima (CC0)\n\n" +
      "<color=#E8C27A>Items</color>\nPlatformer Items free assets\n\n" +
      "<color=#E8C27A>Music</color>\n\"Bonfires\" dark fantasy music pack\n\n" +
      "<color=#E8C27A>Engine</color>\nUnity 6\n\n\n" +
      "<size=120%>Thank you for playing.</size>\n\n<size=80%><color=#A9A3AA>Press Esc to return to the menu</color></size>";
    t.fontSize=34; t.alignment=TextAlignmentOptions.Top; t.textWrappingMode=TextWrappingModes.Normal;
    var rt=(RectTransform)t.transform; rt.sizeDelta=new Vector2(1500, 2600);
    EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s); return "credits written";
  }
}
