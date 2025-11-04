using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    public GameObject inventoryPanel; //The panel that holds the inventory slots (Content)
    public Slot slotPrefab; //Array of all the slots in the inventory
    public int slotCount; //Index of the currently selected slot
    public GameObject[] brickPrefabs; //Array of all the brick prefabs available in the game
    public GameObject pickablePanel;  // <-- assign your left panel here
    public ScrollRect scrollRect; // Assign the Scroll View Horizontal's ScrollRect component
    private int lastFilledSlotIndex = -1;

    void Start()
    {
        // create the requested number of slots
        for (int i = 0; i < slotCount; i++)
        {
            // instantiate a slot under the inventory panel
            Slot slot = Instantiate(slotPrefab, inventoryPanel.transform, false);
        }

        // spawn bricks into the LEFT pickable panel
        if (pickablePanel != null && brickPrefabs != null)
        {
            for (int j = 0; j < brickPrefabs.Length; j++)
            {
                var prefab = brickPrefabs[j];
                if (prefab == null) continue;

                // place each brick under the left panel (do NOT keep world position)
                GameObject brick = Instantiate(prefab, pickablePanel.transform, false);

                // normalize RectTransform so layout works nicely
                var rt = brick.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // if PickablePanel uses a LayoutGroup, these let the layout control size/position
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }
                else
                {
                    // non-UI fallback
                    brick.transform.localPosition = Vector3.zero;
                    brick.transform.localScale = Vector3.one;

                }
            }
        }
    }

    //Mads don't asked me why this is in LateUpdate, it just works better for some reason???
    void LateUpdate()
    {
        // Check if a new brick has been placed and scroll to it
        CheckAndScrollToNewestBrick();
    }

    private void CheckAndScrollToNewestBrick()
    {
        if (inventoryPanel == null || scrollRect == null) return;

        // Find the rightmost filled slot
        int rightmostFilledIndex = -1;
        for (int i = 0; i < inventoryPanel.transform.childCount; i++)
        {
            var slot = inventoryPanel.transform.GetChild(i).GetComponent<Slot>();
            if (slot != null && slot.brickPrefab != null)
            {
                rightmostFilledIndex = i;
            }
        }

        // If a new brick was placed (index changed), scroll to it
        if (rightmostFilledIndex != lastFilledSlotIndex && rightmostFilledIndex >= 0)
        {
            lastFilledSlotIndex = rightmostFilledIndex;
            // Scroll to show the filled brick AND the next empty slot (clamped to valid range)
            int targetIndex = Mathf.Clamp(rightmostFilledIndex + 1, 0, Mathf.Max(0, inventoryPanel.transform.childCount - 1));
            StartCoroutine(ScrollToSlotDelayed(targetIndex));
        }
    }

    private System.Collections.IEnumerator ScrollToSlotDelayed(int slotIndex)
    {
        // Wait for end of frame to ensure layout is updated
        yield return new WaitForEndOfFrame();
        
        if (scrollRect == null) yield break;

        // Rebuild layout to get accurate measurements
        if (inventoryPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryPanel.GetComponent<RectTransform>());
        }

        // Calculate position to show the newest brick fully inside the viewport
        RectTransform contentRect = inventoryPanel.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport;
        
        if (contentRect != null && viewportRect != null && slotIndex >= 0 && slotIndex < inventoryPanel.transform.childCount)
        {
            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;
            
            if (contentWidth > viewportWidth)
            {
                RectTransform targetSlot = inventoryPanel.transform.GetChild(slotIndex) as RectTransform;
                if (targetSlot != null)
                {
                    // Get the rightmost position of the target slot
                    float slotRightEdge = Mathf.Abs(targetSlot.anchoredPosition.x) + targetSlot.rect.width;
                    
                    // Calculate how much we need to scroll to fit this slot on screen
                    float scrollRange = contentWidth - viewportWidth;
                    
                    // We want the slot's right edge to be visible, so scroll until it fits
                    float targetScrollPosition = slotRightEdge - viewportWidth;
                    
                    // Normalize the position (0 to 1)
                    float normalizedPosition = Mathf.Clamp01(targetScrollPosition / scrollRange);
                    
                    scrollRect.horizontalNormalizedPosition = normalizedPosition;
                    yield break;
                }
            }
        }
        
        // Fallback: scroll all the way to the right
        scrollRect.horizontalNormalizedPosition = 1f;
    }
}