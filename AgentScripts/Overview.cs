using UnityEngine; using UnityEditor.SceneManagement;
public static class Overview {
  public static string Run(string scenePath, string tag, float x0, float x1, float cy, float half){
    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    float w=x1-x0; int pw=1800, ph=(int)(pw*(half*2)/w);
    return Shot.Render((x0+x1)/2f, cy, half, pw, ph, "AgentScripts/shots/ov_"+tag+".png");
  }
}
