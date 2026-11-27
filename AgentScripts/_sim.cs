using System.Text;
using UnityEngine;
using UnityEditor.SceneManagement;
public static class SimTest {
  // Run a player-sized probe rightwards across the level with auto-jump; report fall-outs and penetrations.
  public static string Run(float startX, float endX, float speed){
    var sb=new StringBuilder();
    var player=GameObject.FindGameObjectWithTag("Player"); var pb=player.GetComponent<BoxCollider2D>();
    var prb=player.GetComponent<Rigidbody2D>();
    var old=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
    var go=new GameObject("__probe"); go.layer=player.layer; go.transform.localScale=player.transform.localScale;
    var rb=go.AddComponent<Rigidbody2D>(); rb.gravityScale=prb.gravityScale; rb.freezeRotation=true; rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous; rb.interpolation=RigidbodyInterpolation2D.None;
    var bc=go.AddComponent<BoxCollider2D>(); bc.excludeLayers=~LayerMask.GetMask("Ground"); bc.size=pb.size; bc.offset=pb.offset; bc.sharedMaterial=new PhysicsMaterial2D("nf"){friction=0f,bounciness=0f};
    int g=LayerMask.GetMask("Ground");
    var disabled=new System.Collections.Generic.List<Collider2D>();
    foreach(var c in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None)) if(c.gameObject.layer==LayerMask.NameToLayer("Enemy") && c.enabled){ c.enabled=false; disabled.Add(c);}
    var h0=Physics2D.Raycast(new Vector2(startX,40),Vector2.down,80,g);
    go.transform.position=new Vector3(startX,h0.point.y+3f,0); Physics2D.SyncTransforms();
    float dt=Time.fixedDeltaTime; int stuck=0; int jumps=2; float lastX=startX; int pen=0; float maxX=startX;
    int steps=0; for(int i=0;i<60000 && rb.position.x<endX;i++){ steps++;
      bool grounded=Physics2D.OverlapBox((Vector2)bc.bounds.center+Vector2.down*(bc.bounds.extents.y+0.05f), new Vector2(bc.bounds.size.x*0.9f,0.1f),0,g);
      float vy=rb.linearVelocity.y;
      var bb=bc.bounds; bool groundAhead=Physics2D.Raycast(new Vector2(bb.max.x+1.2f,bb.min.y+0.2f),Vector2.down,2.5f,g);
      if(grounded){ jumps=2; }
      if(grounded && (stuck>4 || !groundAhead)) { vy=14f; jumps=1; stuck=0; }
      else if(!grounded && jumps>0 && ((rb.linearVelocity.y<-1f && !Physics2D.Raycast(new Vector2(bb.center.x+1f,bb.min.y),Vector2.down,3f,g)) || (stuck>2 && rb.linearVelocity.y<1f))) { vy=14f; jumps=0; }
      rb.linearVelocity=new Vector2(speed,vy);
      Physics2D.Simulate(dt);
      if(Mathf.Abs(rb.position.x-lastX)<speed*dt*0.3f) stuck++; else stuck=0;
      lastX=rb.position.x; maxX=Mathf.Max(maxX,lastX);
      // penetration check: shrunken box overlapping ground
      var b=bc.bounds; if(Physics2D.OverlapBox(b.center,(Vector2)b.size*0.6f,0,g)){ pen++; if(pen<5) sb.Append($"INSIDE@{rb.position.x:F1},{rb.position.y:F1} "); }
      if(rb.position.y< -20f){ sb.Append($"FELL_OUT@x{rb.position.x:F1} "); break; }
      if(stuck>400){ sb.Append($"BLOCKED@{rb.position.x:F1},{rb.position.y:F1} "); break; }
    }
    sb.Append($"| reached x={maxX:F1} y={rb.position.y:F1} steps={steps} start={startX}");
    foreach(var c in disabled) c.enabled=true;
    Object.DestroyImmediate(go); Physics2D.simulationMode=old; Physics2D.SyncTransforms();
    return sb.ToString();
  }
}
public static class SimAll {
  public static string Run(string scene, float x0, float x1){
    EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
    var gates=Object.FindObjectsByType<LockedGate>(FindObjectsSortMode.None);
    return SimTest.Run(x0,x1,8);
  }
}