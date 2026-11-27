using System.Linq; using UnityEditor; using UnityEditor.Build.Reporting; using UnityEngine;
public static class BuildWin {
  public static string Run(){
    PlayerSettings.productName="Vengeance of the Fallen Knight";
    PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
    PlayerSettings.defaultIsNativeResolution=true;
    PlayerSettings.runInBackground=false;
    PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
    // Default the standalone quality to "High" (the project defaulted to Very Low).
    var names=QualitySettings.names; int high=System.Array.IndexOf(names,"High");
    if(high>=0){ QualitySettings.SetQualityLevel(high,true); }
    var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
    var opts=new BuildPlayerOptions{ scenes=scenes, locationPathName="Builds/Windows/Vengeance of the Fallen Knight.exe", target=BuildTarget.StandaloneWindows64, options=BuildOptions.None };
    var r=BuildPipeline.BuildPlayer(opts);
    var s=r.summary; string errs=string.Join(" | ", r.steps.SelectMany(st=>st.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Exception).Select(m=>m.content).Take(5));
    return $"{s.result} size={s.totalSize/1048576}MB time={s.totalTime} errors={s.totalErrors} warnings={s.totalWarnings} scenes={scenes.Length} {errs}";
  }
}
