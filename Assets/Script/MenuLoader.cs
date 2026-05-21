using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuLoader : MonoBehaviour
{
    public void LoadGraveyard()
    {
        SceneManager.LoadScene("Scenes/Graveyard");
    }

    public void LoadWoodcamp()
    {
        SceneManager.LoadScene("Scenes/Woodcamp");
    }

    public void LoadGrove()
    {
        SceneManager.LoadScene("Scenes/Grove");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
