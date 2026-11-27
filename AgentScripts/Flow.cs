using System.Linq; using System.Text; using System.Threading.Tasks; using UnityEngine; using UnityEngine.UI; using UnityEngine.EventSystems; using UnityEngine.SceneManagement;
public static class Flow {
  static string R(bool ok,string w)=>(ok?"PASS ":"FAIL ")+w+"\n";
  static Button B(string p)=>GameObject.Find(p)?.GetComponent<Button>();
  static async Task W(float s)=>await Task.Delay((int)(s*1000));
  static string Roots(){ var dd=new System.Collections.Generic.List<string>(); for(int i=0;i<SceneManager.sceneCount;i++) foreach(var r in SceneManager.GetSceneAt(i).GetRootGameObjects()) dd.Add(r.name); var tmp=new GameObject("__probe"); Object.DontDestroyOnLoad(tmp); var ddl=tmp.scene.GetRootGameObjects().Where(g=>g!=tmp).Select(g=>g.name).ToArray(); Object.Destroy(tmp); return "DDOL["+string.Join(",",ddl)+"]"; }
  public static async Task<string> Menu(){
    var sb=new StringBuilder(); await W(2.5f);
    sb.Append(R(SceneManager.GetActiveScene().name=="Main Menu","menu scene active"));
    sb.Append(R(EventSystem.current!=null,"EventSystem present"));
    var sel=EventSystem.current.currentSelectedGameObject; sb.Append(R(sel!=null && sel.name=="ContinueButton","Continue focused when a save exists (sel="+(sel?sel.name:"none")+")"));
    sb.Append(R(B("Menu Canvas/Main Panel/ContinueButton").interactable,"Continue enabled"));
    // Settings: change music volume
    B("Menu Canvas/Main Panel/SettingsButton").onClick.Invoke(); await W(0.5f);
    var ms=GameObject.Find("Menu Canvas/Settings Panel/Music Slider").GetComponent<Slider>(); ms.value=0.2f; await W(0.2f);
    sb.Append(R(Mathf.Approximately(PlayerPrefs.GetFloat("MusicVolume"),0.2f) && MusicManager.Instance!=null && Mathf.Abs(MusicManager.Instance.musicSource.volume-0.2f)<0.01f,"music slider changes and saves volume ("+MusicManager.Instance?.musicSource.volume+")"));
    var vt=GameObject.Find("Menu Canvas/Settings Panel/Music Value").GetComponent<TMPro.TMP_Text>(); sb.Append(R(vt.text=="20%","value label updates ("+vt.text+")"));
    ms.value=0.35f;
    var q=GameObject.Find("Menu Canvas/Settings Panel/Quality Slider").GetComponent<Slider>(); int ql=QualitySettings.GetQualityLevel(); q.value=Mathf.Min(q.maxValue,ql+1); await W(0.2f); sb.Append(R(QualitySettings.GetQualityLevel()==(int)q.value,"quality selector applies ("+QualitySettings.names[QualitySettings.GetQualityLevel()]+")")); q.value=ql;
    B("Menu Canvas/Settings Panel/Back").onClick.Invoke(); await W(0.5f);
    // Level select
    B("Menu Canvas/Main Panel/LevelSelectButton").onClick.Invoke(); await W(0.5f);
    sb.Append(R(B("Menu Canvas/Level Select Panel/Level 1").interactable && B("Menu Canvas/Level Select Panel/Level 2").interactable && !B("Menu Canvas/Level Select Panel/Level 3").interactable,"level select: I and II unlocked, III sealed"));
    B("Menu Canvas/Level Select Panel/Back").onClick.Invoke(); await W(0.5f);
    // New game with confirm
    B("Menu Canvas/Main Panel/NewGameButton").onClick.Invoke(); await W(0.5f);
    sb.Append(R(GameObject.Find("Menu Canvas/Confirm Panel")!=null,"New Game asks for confirmation when a save exists"));
    B("Menu Canvas/Confirm Panel/Yes").onClick.Invoke(); await W(3.5f);
    sb.Append(R(SceneManager.GetActiveScene().name=="Graveyard","Begin Anew loads the Graveyard"));
    sb.Append(R(!GameSave.HasSavedGame,"new game cleared the old run"));
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); sb.Append(R(p.CurrentHealth==p.maxHealth,"fresh run starts at full health"));
    sb.Append(Roots()+"\n");
    return sb.ToString();
  }
  public static async Task<string> BackToMenu(){
    var sb=new StringBuilder();
    Object.FindFirstObjectByType<PauseMenu>().SetPaused(true); await W(0.4f);
    var mm=GameObject.Find("[L1] HUD Canvas/Pause/Panel/Main Menu").GetComponent<Button>(); mm.onClick.Invoke(); await W(3.5f);
    sb.Append(R(SceneManager.GetActiveScene().name=="Main Menu" && Time.timeScale==1f,"pause -> Main Menu returns to the menu with time running"));
    sb.Append(R(Object.FindObjectsByType<GameHUD>(FindObjectsSortMode.None).Length==0,"no level HUD leaks into the menu"));
    sb.Append(R(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"exactly one EventSystem"));
    sb.Append(Roots()+"\n");
    return sb.ToString();
  }
}
