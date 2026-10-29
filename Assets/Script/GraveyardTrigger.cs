using UnityEngine;

public class GraveyardTrigger : MonoBehaviour
{
    public GameObject somethingToTrigger;
    private bool hasTriggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || other == null || !other.CompareTag("Player") || somethingToTrigger == null)
            return;

        hasTriggered = true;
        somethingToTrigger.SetActive(true);
    }
}
