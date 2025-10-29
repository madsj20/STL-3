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

        // Get the player's current facing direction
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("PlayerController not found!");
            return;
        }

        Vector2Int carFacing = player.faceDirection;
        
        // Get the absolute grid direction the action will move
        Vector2Int gridMoveDir = GetGridMoveDirection(action);
        
        // Convert grid direction to visual screen direction
        Vector2Int visualDir = ConvertGridToVisual(gridMoveDir);
        
        // Show the arrow matching the visual direction
        if (visualDir == Vector2Int.up && upArrow)
        {
            upArrow.SetActive(true);
            SetArrowWorldDirection(upArrow, Vector2Int.up);
        }
        else if (visualDir == Vector2Int.down && downArrow)
        {
            downArrow.SetActive(true);
            SetArrowWorldDirection(downArrow, Vector2Int.down);
        }
        else if (visualDir == Vector2Int.left && leftArrow)
        {
            leftArrow.SetActive(true);
            SetArrowWorldDirection(leftArrow, Vector2Int.left);
        }
        else if (visualDir == Vector2Int.right && rightArrow)
        {
            rightArrow.SetActive(true);
            SetArrowWorldDirection(rightArrow, Vector2Int.right);
        }
    }

    // Set arrow to point in a specific world direction, regardless of parent rotation
    private void SetArrowWorldDirection(GameObject arrow, Vector2Int worldDir)
    {
        // Calculate the world rotation for this direction
        // Assuming arrows are designed to point UP by default (0°)
        float targetAngle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg - 90f;
        
        // Set the arrow's world rotation
        arrow.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        
        // Force the arrow to stay at local position (0,0,0) - centered on parent
        arrow.transform.localPosition = Vector3.zero;
    }

    // Get the grid direction based on the action
    private Vector2Int GetGridMoveDirection(BrickQueManager.ActionType action)
    {
        switch (action)
        {
            case BrickQueManager.ActionType.MoveForward:
                return Vector2Int.up; // Grid Y+

            case BrickQueManager.ActionType.MoveBackward:
                return Vector2Int.down; // Grid Y-

            case BrickQueManager.ActionType.TurnLeft:
                return Vector2Int.left; // Grid X-

            case BrickQueManager.ActionType.TurnRight:
                return Vector2Int.right; // Grid X+

            default:
                return Vector2Int.zero;
        }
    }

    // Convert grid coordinates to visual screen direction
    // Grid Up (0,1) = Visual Up on screen
    // Grid Right (1,0) = Visual Right on screen
    private Vector2Int ConvertGridToVisual(Vector2Int gridDir)
    {
        // In Unity's 2D default:
        // Grid (0, 1) = screen UP
        // Grid (0, -1) = screen DOWN
        // Grid (-1, 0) = screen LEFT
        // Grid (1, 0) = screen RIGHT
        
        // Direct mapping - grid direction IS visual direction
        return gridDir;
    }

    public void HideAll()
    {
        if (upArrow) upArrow.SetActive(false);
        if (downArrow) downArrow.SetActive(false);
        if (leftArrow) leftArrow.SetActive(false);
        if (rightArrow) rightArrow.SetActive(false);
    }
}