using UnityEngine;
using UnityEngine.UI;

public class CarColorPicker : MonoBehaviour
{
    public Image carColorPreview;
    public Image prevArrow;
    public Image nextArrow;

    // Editable in Inspector
    private Color[] colors = new Color[]
    {
        new Color(1f, 1f, 1f), // white
        new Color(0f, 0.45f, 1f), // blue
        new Color(0f, 1f, 0f), // green
        new Color(1f, 0f, 0f), // red
        new Color(1f, 0.92f, 0.016f), // yellow
        new Color(1f, 0.41f, 0.71f) // pink
    };

    public string[] colorNames = new string[] { "White", "Blue", "Green", "Red", "Yellow", "Pink" };

    private int currentIndex = 0;
    private const string PrefIndexKey = "CarColorIndex";
    private const string PrefRKey = "CarColor_R";
    private const string PrefGKey = "CarColor_G";
    private const string PrefBKey = "CarColor_B";

    private void Start()
    {
        if (colors == null || colors.Length == 0) return;

        // Always restore saved index first (clamped).
        currentIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefIndexKey, 0), 0, colors.Length - 1);

        // If separate RGB values were saved previously, restore them.
        if (PlayerPrefs.HasKey(PrefRKey) && PlayerPrefs.HasKey(PrefGKey) && PlayerPrefs.HasKey(PrefBKey))
        {
            float r = PlayerPrefs.GetFloat(PrefRKey, 0f);
            float g = PlayerPrefs.GetFloat(PrefGKey, 0f);
            float b = PlayerPrefs.GetFloat(PrefBKey, 0f);
            ApplyColor(new Color(r, g, b, 1f));
        }
        else
        {
            // Fallback to saved index (or default 0) and apply that color from the list.
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

        // Update arrow colors based on current index (use palette neighbors)
        UpdateArrowColors();

        // Save RGB separately as requested
        PlayerPrefs.SetFloat(PrefRKey, color.r);
        PlayerPrefs.SetFloat(PrefGKey, color.g);
        PlayerPrefs.SetFloat(PrefBKey, color.b);
    }

    private void UpdateArrowColors()
    {
        if (colors == null || colors.Length == 0) return;

        int len = colors.Length;
        int prevIndex = (currentIndex - 1 + len) % len;
        int nextIndex = (currentIndex + 1) % len;

        if (prevArrow != null)
            prevArrow.color = colors[prevIndex];

        if (nextArrow != null)
            nextArrow.color = colors[nextIndex];
    }
}