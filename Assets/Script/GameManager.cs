using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Animator gameOverAnimator;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerGameOverBackground()
    {
        gameOverAnimator.SetTrigger("Show");
    }
}
