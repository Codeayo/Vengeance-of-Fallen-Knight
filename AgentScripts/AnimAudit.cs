using System.Linq; using System.Text; using UnityEditor; using UnityEditor.Animations; using UnityEngine;
public static class AnimAudit {
  public static string Run(){
    var sb=new StringBuilder();
    foreach(var g in AssetDatabase.FindAssets("t:AnimatorController")){
      var p=AssetDatabase.GUIDToAssetPath(g); var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(p);
      sb.AppendLine("## "+p+" params="+string.Join(",",c.parameters.Select(x=>x.name+":"+x.type.ToString()[0])));
      foreach(var l in c.layers){ var sm=l.stateMachine; sb.AppendLine("  default="+sm.defaultState?.name);
        foreach(var s in sm.states){ var clip=s.state.motion as AnimationClip; sb.AppendLine($"  [{s.state.name}] clip={(clip?clip.name+" "+clip.length.ToString("F2")+"s loop="+clip.isLooping:"none")} speed={s.state.speed}");
          foreach(var t in s.state.transitions) sb.AppendLine($"     -> {t.destinationState?.name} exit={t.hasExitTime}@{t.exitTime:F2} dur={t.duration:F2} cond={string.Join("&",t.conditions.Select(k=>k.parameter+k.mode.ToString()[0]+k.threshold))}"); }
        foreach(var t in sm.anyStateTransitions) sb.AppendLine($"  ANY -> {t.destinationState?.name} cond={string.Join("&",t.conditions.Select(k=>k.parameter+k.mode.ToString()[0]+k.threshold))} exit={t.hasExitTime}"); }
    }
    var mm=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/SceneManagement.prefab");
    sb.AppendLine("AUDIO: "+string.Join(", ",AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)));
    sb.AppendLine("MIXERS: "+AssetDatabase.FindAssets("t:AudioMixer").Length);
    return sb.ToString();
  }
}
