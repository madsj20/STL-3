using UnityEngine;
using UnityEngine.UI;

public class CarColorPicker : MonoBehaviour
{
    public Image carColorPreview;

    // Editable in Inspector
    private Color[] colors = new Color[]
    {
        new Color(1, 1, 1), //white
        new Color(0, 0.45f, 1), // blue
        new Color(0, 1, 0), // green
        new Color(1, 0, 0), // red
        new Color(1f, 0.92f, 0.016f), // yellow
        new Color(1f, 0.41f, 0.71f) // pink
    };

    public string[] colorNames = new string[] { "Blue", "Green", "Red", "Yellow", "Pink" };

    private int currentIndex = 0;
    private const string PrefIndexKey = "CarColorIndex";
    private const string PrefRKey = "CarColor_R";
    private const string PrefGKey = "CarColor_G";
    private const string PrefBKey = "CarColor_B";

    private void Start()
    {
        if (colors == null || colors.Length == 0) return;

        // If separate RGB values were saved previously, restore them.
        if (PlayerPrefs.HasKey(PrefRKey) && PlayerPrefs.HasKey(PrefGKey) && PlayerPrefs.HasKey(PrefBKey))
        {
            float r = PlayerPrefs.GetFloat(PrefRKey, 0f);
            float g = PlayerPrefs.GetFloat(PrefGKey, 0f);
            float b = PlayerPrefs.GetFloat(PrefBKey, 0f);
            ApplyColor(new Color(r, g, b, 1f));
            // Keep currentIndex as the saved index if present, otherwise 0
            currentIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefIndexKey, 0), 0, colors.Length - 1);
        }
        else
        {
            // Fallback to saved index (or default 0)
            currentIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefIndexKey, 0), 0, colors.Length - 1);
            ApplyCurrentColor();
        }
    }

    // Wire these two to your Previous / Next buttons
    public void Previous()
    {
        if (colors == null || colors.Length == 0) return;
        currentIndex = (currentIndex - 1 + colors.Length) % colors.Length;
        ApplyCurrentColor();
    }

    public void Next()
    {
        if (colors == null || colors.Length == 0) return;
        currentIndex = (currentIndex + 1) % colors.Length;
        ApplyCurrentColor();
    }

    private void ApplyCurrentColor()
    {
        ApplyColor(colors[currentIndex]);

        // Save the selected index for convenience/backwards compatibility
        PlayerPrefs.SetInt(PrefIndexKey, currentIndex);

        // Optional: save a readable name as before
        if (colorNames != null && currentIndex < colorNames.Length)
            PlayerPrefs.SetString("CarColor", colorNames[currentIndex]);

        PlayerPrefs.Save();
    }

    private void ApplyColor(Color color)
    {
        if (carColorPreview != null) carColorPreview.color = color;

        // Save RGB separately as requested
        PlayerPrefs.SetFloat(PrefRKey, color.r);
        PlayerPrefs.SetFloat(PrefGKey, color.g);
        PlayerPrefs.SetFloat(PrefBKey, color.b);
    }
}