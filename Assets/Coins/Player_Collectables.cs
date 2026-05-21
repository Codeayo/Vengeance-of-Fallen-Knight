using UnityEngine;
using UnityEngine.UI;

public class Player_Collectables : MonoBehaviour
{
    private int currentCoins;

    public Text currentCoin_Text;
   
    private void Start()
    {
        currentCoins = 0;

        if (currentCoin_Text != null)
            currentCoin_Text.text = "0";
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // --- Coins ---
        if (collision.CompareTag("Coin"))
        {
            currentCoins++;

            if (currentCoin_Text != null)
                currentCoin_Text.text = currentCoins.ToString();

            // Disable coin collider so it can't be collected twice
            CircleCollider2D col = collision.GetComponent<CircleCollider2D>();
            if (col != null)
                col.enabled = false;

            // Play coin animation
            Animator anim = collision.transform.GetChild(0)?.GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("Collect");

            // Destroy after animation plays
            Destroy(collision.gameObject, 1f);
        }
    }
}
