using UnityEngine;

public class GraveyardTrigger : MonoBehaviour
{
    public GameObject somethingToTrigger;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && somethingToTrigger != null)
        {
            somethingToTrigger.SetActive(true);
        }
    }
}
