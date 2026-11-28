using UnityEngine; using UnityEditor.SceneManagement;
public static class ListEn { public static string Run(){ EditorSceneManager.OpenScene("Assets/Scenes/Graveyard.unity"); var e=GameObject.Find("Enemies"); if(e==null) return "no Enemies root"; string r=""; foreach(Transform t in e.transform) r+=t.name+"; "; return r; } }
