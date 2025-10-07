using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public GameObject brickPrefab;
    public Image backgroundImage;

    private void Start()
    {
        // baggrund er OFF fra start
        SetBackgroundActive(false);
    }

    public void SetBackgroundActive(bool active)
    {
        if (backgroundImage != null)
            backgroundImage.enabled = active;
    }

    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }
}
