using System.Linq; using System.Text; using UnityEditor; using UnityEngine;
public static class BuildAudio {
  static AudioClip M(string n)=>AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Music/Bonfires - dark fantasy music/WAV_{n}.wav");
  static AudioCatalog.SceneAudio S(string scene,string music,string amb,string[] shots,float a,float b,string step)=>new AudioCatalog.SceneAudio{scene=scene,music=M(music),ambience=amb,ambientOneShots=shots,oneShotMinGap=a,oneShotMaxGap=b,footstep=step};
  public static string Build(){
    string path="Assets/Resources/AudioCatalog.asset"; var c=AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
    if(c==null){ c=ScriptableObject.CreateInstance<AudioCatalog>(); AssetDatabase.CreateAsset(c,path); }
    c.scenes=new System.Collections.Generic.List<AudioCatalog.SceneAudio>{
      S("Main Menu","Bonfires","amb_menu",new[]{"raven"},14,30,"step_dirt"),
      S("Graveyard","Nightfall","amb_graveyard",new[]{"raven","thunder","ghost_whisper","raven"},12,26,"step_stone"),
      S("Woodcamp","Mistridden_Woods","amb_camp",new[]{"bird","raven","bird"},7,16,"step_dirt"),
      S("Grove","Voices_From_The_Grove","amb_grove",new[]{"ghost_whisper","drip","ghost_whisper"},9,20,"step_dirt"),
      S("Taiga","Ruins_In_The_Deep","amb_taiga",new[]{"ice_crack","raven"},10,22,"step_snow"),
      S("Castle","Necrons_Tower","amb_castle",new[]{"chain","door_creak","drip","chain"},8,18,"step_stone"),
      S("ThroneRoom","Temple_Of_Illas","amb_throne",new[]{"chain","ghost_whisper"},14,28,"step_stone"),
      S("Credits","Bonfires","amb_menu",new string[0],0,0,"step_dirt"),
    };
    c.defaultBossMusic=M("Cursemark");
    c.bosses=new System.Collections.Generic.List<AudioCatalog.BossMusic>{ new AudioCatalog.BossMusic{boss="Lord Malvric, the Usurper",music=M("Tyrants_Lament")}, new AudioCatalog.BossMusic{boss="Sir Dravek, Captain of the Black Guard",music=M("Tyrants_Lament")} };
    c.openingMusic=M("Nightfall"); c.epilogueMusic=M("Bonfires");
    EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
    return "catalog: "+c.scenes.Count(x=>x.music!=null)+"/"+c.scenes.Count+" scene tracks";
  }
  public static string Verify(){
    var sb=new StringBuilder(); var t=System.Diagnostics.Stopwatch.StartNew();
    var f=typeof(SoundLibrary).GetField("recipes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
    SoundLibrary.IsLoop("x"); var keys=((System.Collections.IDictionary)f.GetValue(null)).Keys.Cast<string>().ToList(); int bad=0;
    foreach(var k in keys){ var clips=SoundLibrary.GetAll(k); foreach(var cl in clips){ var d=new float[cl.samples]; cl.GetData(d,0); float rms=Mathf.Sqrt(d.Select(x=>x*x).Average()); bool nan=d.Any(x=>float.IsNaN(x)); if(nan||rms<0.005f){ bad++; sb.Append($"BAD {k} rms={rms:F4} nan={nan}; ");} } }
    return $"{keys.Count} sounds generated in {t.ElapsedMilliseconds}ms, problems={bad} {sb}";
  }
}
