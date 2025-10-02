using UnityEngine;

public class ArrowHintController : MonoBehaviour
{
    public static ArrowHintController Instance { get; private set; }

    [Header("Directional arrows (leave disabled by default)")]
    public GameObject upArrow;
    public GameObject downArrow;
    public GameObject leftArrow;
    public GameObject rightArrow;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HideAll();
    }

    public void ShowForAction(BrickQueManager.ActionType action)
    {
        HideAll(); // only one at a time

        switch (action)
        {
            // your current mapping; adjust if you use MoveUp/MoveDown/MoveLeft/MoveRight
            case BrickQueManager.ActionType.MoveForward:
            case BrickQueManager.ActionType.MoveUp:
                if (upArrow) upArrow.SetActive(true);
                break;

            case BrickQueManager.ActionType.MoveBackward:
            case BrickQueManager.ActionType.MoveDown:
                if (downArrow) downArrow.SetActive(true);
                break;

            case BrickQueManager.ActionType.TurnLeft:
            case BrickQueManager.ActionType.MoveLeft:
                if (leftArrow) leftArrow.SetActive(true);
                break;

            case BrickQueManager.ActionType.TurnRight:
            case BrickQueManager.ActionType.MoveRight:
                if (rightArrow) rightArrow.SetActive(true);
                break;

            default:
                // None or unknown -> keep hidden
                break;
        }
        // Important: we do NOT touch any Animator/“pulse” script on these objects.
        // When SetActive(true) they resume their existing pulsing logic.
    }

    public void HideAll()
    {
        if (upArrow) upArrow.SetActive(false);
        if (downArrow) downArrow.SetActive(false);
        if (leftArrow) leftArrow.SetActive(false);
        if (rightArrow) rightArrow.SetActive(false);
    }
}
