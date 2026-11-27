using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement;
public static class MakeAxe {
  public static string Run(){
    var scene=EditorSceneManager.OpenScene("Assets/Scenes/Graveyard.unity", OpenSceneMode.Single);
    var axe=GameObject.Find("Enemies/Axe Skeleton"); if(axe==null) return "no axe";
    if(PrefabUtility.IsPartOfPrefabInstance(axe)) return "already prefab: "+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(axe);
    PrefabUtility.SaveAsPrefabAssetAndConnect(axe,"Assets/Enemies/Prefabs/AxeSkeleton.prefab",InteractionMode.AutomatedAction);
    EditorSceneManager.SaveScene(scene); return "saved AxeSkeleton.prefab";
  }
}
