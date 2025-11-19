using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public GameObject brickPrefab;
    public Image backgroundImage;
    public Image speedBorderImage; // New: border image for speed boost indicator
    public bool squeezeUsedThisDrag = false;

    private void Awake()
    {
        // If speedBorderImage is not assigned, try to find it
        if (speedBorderImage == null)
        {
            Transform borderTransform = transform.Find("SpeedBorder");
            if (borderTransform != null)
            {
                speedBorderImage = borderTransform.GetComponent<Image>();
            }
        }
    }

    private void Start()
    {
        // baggrund er OFF fra start
        SetBackgroundActive(false);
        SetSpeedBorderActive(false);
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

    // New methods for speed boost border
    public void SetSpeedBorderActive(bool active)
    {
        if (speedBorderImage != null)
        {
            speedBorderImage.enabled = active;
        }
    }

    public void SetSpeedBorderColor(Color color)
    {
        if (speedBorderImage != null)
            speedBorderImage.color = color;
    }
}