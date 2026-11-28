using System.Reflection; using System.Text; using System.Threading.Tasks; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.InputSystem.LowLevel; using UnityEngine.SceneManagement;
public static class SkipTest {
  static string R(bool ok,string w)=>(ok?"PASS ":"FAIL ")+w+"\n";
  public static async Task<string> Run(){
    var sb=new StringBuilder();
    for(int i=0;i<60 && !Cinematic.IsPlaying;i++) await Task.Delay(100);
    sb.Append(R(Cinematic.IsPlaying && OpeningCinematic.Running,"opening cinematic starts on New Game"));
    await Task.Delay(4000);
    var kb=Keyboard.current; InputSystem.QueueStateEvent(kb,new KeyboardState(Key.Escape));
    float t0=Time.realtimeSinceStartup; for(int i=0;i<60 && Cinematic.IsPlaying;i++) await Task.Delay(100);
    InputSystem.QueueStateEvent(kb,new KeyboardState());
    sb.Append(R(!Cinematic.IsPlaying,$"holding Esc skips it ({Time.realtimeSinceStartup-t0:F1}s)"));
    await Task.Delay(800);
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>(); var sr=p.GetComponent<SpriteRenderer>();
    var col=p.GetComponent<Collider2D>(); var g=Physics2D.Raycast(new Vector2(col.bounds.center.x,col.bounds.max.y),Vector2.down,20,LayerMask.GetMask("Ground"));
    sb.Append(R(rb.simulated && !Player.ControlLocked && sr.sortingLayerName=="player" && g && Mathf.Abs(col.bounds.min.y-g.point.y)<0.25f,"after skipping, the knight stands at his grave with control"));
    sb.Append(R(Object.FindObjectsByType<GhostNarrator>(FindObjectsSortMode.None).Length==0,"ghost removed after skip"));
    // death and Rise Again
    p.TakeDamage(9999,true); await Task.Delay(3500);
    sb.Append(R(p.IsDead && GameHUD.IsGameOverShowing,"death plays and shows Game Over"));
    sb.Append(R(Mathf.Abs(rb.position.y - g.point.y) < 3f,$"body stays on the ground while dead (y={rb.position.y:F1})"));
    var rise=(UnityEngine.UI.Button)typeof(GameHUD).GetField("riseAgainButton",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(GameHUD.Instance);
    rise.onClick.Invoke(); await Task.Delay(2500);
    sb.Append(R(!p.IsDead && rb.bodyType==RigidbodyType2D.Dynamic && !Cinematic.IsPlaying && !OpeningCinematic.Running,"Rise Again revives without replaying the opening"));
    // audio
    var mm=MusicManager.Instance; var fs=typeof(MusicManager).GetField("ambientA",BindingFlags.NonPublic|BindingFlags.Instance); var fb=typeof(MusicManager).GetField("ambientB",BindingFlags.NonPublic|BindingFlags.Instance);
    var a=(AudioSource)fs.GetValue(mm); var b=(AudioSource)fb.GetValue(mm);
    sb.Append(R(mm.musicSource.isPlaying,$"music playing ({mm.musicSource.clip?.name}, vol {mm.musicSource.volume:F2})"));
    sb.Append(R(a.isPlaying||b.isPlaying,$"ambience playing ({(a.isPlaying?a.clip.name:b.clip?.name)})"));
    float before=AudioListener.volume; GameSettings.MasterVolume=0.4f; sb.Append(R(Mathf.Approximately(AudioListener.volume,0.4f),"master volume applies")); GameSettings.MasterVolume=before;
    int playing=0; foreach(var s in mm.GetComponents<AudioSource>()) if(s.isPlaying) playing++;
    sb.Append($"     audio sources currently playing: {playing}\n");
    return sb.ToString();
  }
}
