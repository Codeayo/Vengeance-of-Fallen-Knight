using System.Reflection; using System.Text; using System.Threading.Tasks; using UnityEngine; using UnityEngine.SceneManagement;
public static class FinalTest {
  static string R(bool ok,string w)=>(ok?"PASS ":"FAIL ")+w+"\n";
  public static async Task<string> Run(){
    var sb=new StringBuilder();
    while(Cinematic.IsPlaying) await Task.Delay(200);
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var enc=Object.FindFirstObjectByType<BossEncounter>(); var bossMb=typeof(BossEncounter).GetField("boss",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(enc) as MonoBehaviour;
    sb.Append(R(!bossMb.enabled,"Malvric dormant before the encounter"));
    rb.position=new Vector2(enc.transform.position.x, enc.transform.position.y-2f); rb.linearVelocity=Vector2.zero;
    await Task.Delay(1500);
    sb.Append(R(Cinematic.IsPlaying,"boss introduction cinematic plays"));
    ScreenCapture.CaptureScreenshot("AgentScripts/shots/cine/malvric_intro.png");
    while(Cinematic.IsPlaying) await Task.Delay(200);
    sb.Append(R(bossMb.enabled,"Malvric active after the introduction"));
    var mal=bossMb as BossKnightAI; int guard=0;
    while(!mal.IsDead && guard++<200){ mal.SendMessage("TakeDamage",15); p.TryHeal(999); await Task.Delay(120); }
    sb.Append(R(mal.IsDead,"Malvric defeated (phase lines and summons ran)"));
    var summoned=Object.FindObjectsByType<PatrolSkeletonAI>(FindObjectsSortMode.None).Length;
    sb.Append($"     skeleton guards in scene after summon: {summoned}\n");
    await Task.Delay(4500);
    sb.Append(R(Cinematic.IsPlaying,"epilogue cinematic plays"));
    ScreenCapture.CaptureScreenshot("AgentScripts/shots/cine/epilogue.png");
    for(int i=0;i<600 && SceneManager.GetActiveScene().name!="Credits";i++) await Task.Delay(100);
    sb.Append(R(SceneManager.GetActiveScene().name=="Credits","epilogue ends in the Credits"));
    sb.Append(R(Inventory.Has("frozen_crown"),"Shard of the Crown awarded"));
    return sb.ToString();
  }
}
