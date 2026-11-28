using System.Threading.Tasks; using UnityEngine;
public static class BossWalk {
  public static async Task<string> Go(float x){
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var hit=Physics2D.Raycast(new Vector2(x,80),Vector2.down,200,LayerMask.GetMask("Ground"));
    rb.position=new Vector2(x,hit.point.y+1.65f); rb.linearVelocity=Vector2.zero; p.TryHeal(999);
    await Task.Delay(300); return $"cinematic={Cinematic.IsPlaying} locked={Player.ControlLocked}";
  }
  public static string State(){
    var enc=Object.FindFirstObjectByType<BossEncounter>(); var f=typeof(BossEncounter).GetField("boss",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    var b=enc!=null? f.GetValue(enc) as MonoBehaviour : null;
    return $"cine={Cinematic.IsPlaying} bossAI={(b? b.enabled.ToString():"gone")} music={MusicManager.Instance.musicSource.clip?.name}";
  }
}
