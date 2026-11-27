using System.Threading.Tasks; using UnityEngine; using UnityEngine.SceneManagement;
public static class Tour2 {
  public static async Task<string> Go(string scene, float x, float wait){
    if(ReadPanel.IsOpen){ typeof(ReadPanel).GetProperty("IsOpen").SetValue(null,false); Time.timeScale=1f; }
    if(SceneManager.GetActiveScene().name!=scene){ SceneFader.LoadScene(scene); for(int i=0;i<80 && SceneManager.GetActiveScene().name!=scene;i++) await Task.Delay(100); await Task.Delay(2500); }
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var hit=Physics2D.Raycast(new Vector2(x,80),Vector2.down,200,LayerMask.GetMask("Ground"));
    rb.position=new Vector2(x,hit.point.y+1.65f); p.transform.position=rb.position; rb.linearVelocity=Vector2.zero; p.TryHeal(999);
    await Task.Delay((int)(wait*1000)); return scene+" at "+x;
  }
}
