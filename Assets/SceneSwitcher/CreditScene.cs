using UnityEngine;

public class CreditScene : MonoBehaviour
{
    public float scrollSpeed = 40f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
    }
}
