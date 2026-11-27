using UnityEditor.SceneManagement; using UnityEngine;
public static class SimAll {
  public static string Run(string scene, float x0, float x1){
    EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
    var gates=Object.FindObjectsByType<LockedGate>(FindObjectsSortMode.None);
    return SimTest.Run(x0,x1,8);
  }
}
