using UnityEditor; using UnityEngine;
public static class BuildCineAssets {
  public static string Build(){
    UIKit.EnsureGenerated();
    string path="Assets/Resources/CinematicAssets.asset"; var a=AssetDatabase.LoadAssetAtPath<CinematicAssets>(path);
    if(a==null){ a=ScriptableObject.CreateInstance<CinematicAssets>(); AssetDatabase.CreateAsset(a,path); }
    a.ghostFigure=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Salt.png");
    a.softGlow=UIKit.Gen("SoftCircle"); a.mist=UIKit.Gen("Mist");
    a.particleMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/ParticleSoft.mat");
    EditorUtility.SetDirty(a); AssetDatabase.SaveAssets();
    return $"ghost={a.ghostFigure!=null} glow={a.softGlow!=null} mat={a.particleMaterial!=null}";
  }
}
