using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    [Header("Transition Settings")]
    public Animator transition;
    public float transitionTime = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) LoadLevel("Main Menu");
        if (Input.GetKeyDown(KeyCode.Alpha2)) LoadLevel("Graveyard");
        if (Input.GetKeyDown(KeyCode.Alpha3)) LoadLevel("Woodcamp");
        if (Input.GetKeyDown(KeyCode.Alpha4)) LoadLevel("Grove");
        if (Input.GetKeyDown(KeyCode.Alpha5)) LoadLevel("CastleHallScene");
    }

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (transition != null)
            transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(sceneName);
    }
}
