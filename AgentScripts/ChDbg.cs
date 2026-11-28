using UnityEngine;
public static class ChDbg {
  public static string Run(){
    var c=Object.FindFirstObjectByType<ChapterIntro>(FindObjectsInactive.Include); var p=GameObject.FindGameObjectWithTag("Player");
    var startPos = typeof(Player).GetField("respawnPoint",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(p.GetComponent<Player>());
    return c==null? "no ChapterIntro" : $"intro at {c.transform.position} active={c.isActiveAndEnabled} player={p.transform.position} spawn={startPos} cineInstance={(Cinematic.Instance!=null)}";
  }
}
