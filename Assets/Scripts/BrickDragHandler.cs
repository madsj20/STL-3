using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class BrickDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;
    int originalSiblingIndex;
    BrickPiece pieceData;

    // Squeeze-in detection
    private Slot hoveredSlot;
    private Coroutine squeezeCoroutine;
    private const float SQUEEZE_DELAY = 0.5f; // Time to hold before squeeze-in
    private bool squeezeTriggered = false;

    // ---- NEW: visual insertion marker state (purely visual, no logic change) ----
    private GameObject insertionMarker;                 // thin vertical line
    private Color highlightColor = new Color(1f, 0.2f, 0f, 0.9f); // bright orange
    private bool insertAfter;                           // true = right edge, false = left edge
    private int pendingInsertIndex = -1;               // computed while hovering (visual only)
    // ---------------------------------------------------------------------------

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        pieceData = GetComponent<BrickPiece>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Reset squeeze state
        squeezeTriggered = false;
        hoveredSlot = null;
        if (squeezeCoroutine != null)
        {
            StopCoroutine(squeezeCoroutine);
            squeezeCoroutine = null;
        }

        // Normalize RectTransform for dragging
        var rt = GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        // Save the original parent (slot or panel)
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Create a replacement brick in the toolbox if dragging from there
        if (originalParent != null && originalParent.CompareTag("Toolbox"))
        {
            var replacement = Instantiate(gameObject, originalParent, false);
            replacement.transform.SetSiblingIndex(originalSiblingIndex);

            // Make sure the replacement participates in the LayoutGroup
            var repRt = replacement.GetComponent<RectTransform>();
            var repLe = replacement.GetComponent<UnityEngine.UI.LayoutElement>();
            if (repLe) repLe.ignoreLayout = false;
            if (repRt)
            {
                repRt.localScale = Vector3.one;
                repRt.anchorMin = repRt.anchorMax = new Vector2(0.5f, 0.5f);
                repRt.pivot = new Vector2(0.5f, 0.5f);
                repRt.anchoredPosition = Vector2.zero;
                repRt.localRotation = Quaternion.identity;
            }
        }

        var piece = GetComponent<BrickPiece>();
        if (piece != null && ArrowHintController.Instance != null)
        {
            ArrowHintController.Instance.ShowForAction(piece.action);
        }

        // Re-parent to the nearest Canvas so the brick can be dragged across all UI
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) transform.SetParent(canvas.transform, true);

        // Make the brick semi-transparent and allow raycasts to pass through
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;
        }

        // ---- NEW: create the insertion marker (hidden until we hover a slot) ----
        CreateInsertionMarker();
    }

    public void OnDrag(PointerEventData eventData)
    {
        var rt = GetComponent<RectTransform>();
        if (!rt) { transform.position = eventData.position; return; }

        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out worldPos);
        rt.position = worldPos;

        // Check if hovering over a slot OR find nearest slot for squeeze-in
        var hit = eventData.pointerCurrentRaycast.gameObject;
        Slot currentSlot = hit ? hit.GetComponentInParent<Slot>() : null;
        Slot originalSlot = originalParent ? originalParent.GetComponent<Slot>() : null;

        // If not directly over a slot, try to find the nearest slot for squeeze-in
        if (currentSlot == null)
        {
            currentSlot = FindNearestSlot(eventData.position, eventData.pressEventCamera);
        }

        // ---- NEW: show a thin vertical insertion line on left/right edge ----
        if (currentSlot != null)
        {
            pendingInsertIndex = currentSlot.transform.GetSiblingIndex();
            ShowInsertionMarker(currentSlot);
        }
        else
        {
            HideInsertionMarker();
            pendingInsertIndex = -1;
        }
        // --------------------------------------------------------------------

        // Only trigger squeeze-in for occupied slots (and don't squeeze into your own original slot)
        if (currentSlot != null && currentSlot.brickPrefab != null && currentSlot != hoveredSlot && currentSlot != originalSlot)
        {
            // New slot detected
            hoveredSlot = currentSlot;
            squeezeTriggered = false;

            // Stop previous coroutine if any
            if (squeezeCoroutine != null)
                StopCoroutine(squeezeCoroutine);

            // Start new squeeze timer
            squeezeCoroutine = StartCoroutine(SqueezeInTimer(currentSlot));
        }
        else if (currentSlot != hoveredSlot)
        {
            // Moved away from the slot
            if (squeezeCoroutine != null)
            {
                StopCoroutine(squeezeCoroutine);
                squeezeCoroutine = null;
            }
            hoveredSlot = null;
            squeezeTriggered = false;
        }
    }

    private Slot FindNearestSlot(Vector2 screenPos, Camera camera)
    {
        // Find the inventory panel that contains slots
        var manager = FindFirstObjectByType<BrickQueManager>();
        if (manager == null || manager.PanelThatPlaysTheSequence == null) return null;

        Transform slotsPanel = manager.PanelThatPlaysTheSequence;
        Slot nearestSlot = null;
        float nearestDist = float.MaxValue;
        float maxDetectionDist = 100f; // Maximum distance to detect squeeze-in

        for (int i = 0; i < slotsPanel.childCount; i++)
        {
            var slot = slotsPanel.GetChild(i).GetComponent<Slot>();
            if (slot == null) continue;

            var slotRT = slot.GetComponent<RectTransform>();
            if (slotRT == null) continue;

            // Get slot's screen position
            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(camera, slotRT.position);
            float dist = Vector2.Distance(screenPos, slotScreenPos);

            if (dist < nearestDist && dist < maxDetectionDist)
            {
                nearestDist = dist;
                nearestSlot = slot;
            }
        }

        return nearestSlot;
    }

    private IEnumerator SqueezeInTimer(Slot targetSlot)
    {
        yield return new WaitForSeconds(SQUEEZE_DELAY);

        // After delay, perform the squeeze-in
        if (targetSlot != null && targetSlot.brickPrefab != null)
        {
            PerformSqueezeIn(targetSlot);
            squeezeTriggered = true;
        }
    }

    private void PerformSqueezeIn(Slot targetSlot)
    {
        // Find the parent panel that contains all slots
        Transform slotsPanel = targetSlot.transform.parent;
        if (slotsPanel == null) return;

        int targetIndex = targetSlot.transform.GetSiblingIndex();

        // Check if the last slot is occupied - if so, create a new slot
        var lastSlot = slotsPanel.GetChild(slotsPanel.childCount - 1).GetComponent<Slot>();
        if (lastSlot != null && lastSlot.brickPrefab != null)
        {
            // Need to create a new slot at the end
            var inventoryController = FindFirstObjectByType<InventoryController>();
            if (inventoryController != null && inventoryController.slotPrefab != null)
            {
                Slot newSlot = Instantiate(inventoryController.slotPrefab, slotsPanel, false);
            }
        }

        // Shift all bricks from targetIndex onwards to the right
        for (int i = slotsPanel.childCount - 1; i > targetIndex; i--)
        {
            var currentSlot = slotsPanel.GetChild(i).GetComponent<Slot>();
            var previousSlot = slotsPanel.GetChild(i - 1).GetComponent<Slot>();

            if (currentSlot == null || previousSlot == null) continue;

            // Move brick from previous slot to current slot
            if (previousSlot.brickPrefab != null)
            {
                currentSlot.brickPrefab = previousSlot.brickPrefab;
                currentSlot.brickPrefab.transform.SetParent(currentSlot.transform, false);
                SnapUI(currentSlot.brickPrefab.transform as RectTransform);
            }
        }

        // Clear the target slot (we'll place the dragged brick here on drop)
        targetSlot.brickPrefab = null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Stop squeeze timer
        if (squeezeCoroutine != null)
        {
            StopCoroutine(squeezeCoroutine);
            squeezeCoroutine = null;
        }

        // ---- NEW: clean up visual insertion marker ----
        if (insertionMarker != null)
        {
            Destroy(insertionMarker);
            insertionMarker = null;
        }
        // ----------------------------------------------

        // Hide hint arrow no matter how the drag ends
        if (ArrowHintController.Instance != null)
            ArrowHintController.Instance.HideAll();

        // Restore raycast blocking and full opacity
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        // Find the slot under the drop position
        var hit = eventData.pointerCurrentRaycast.gameObject;
        Slot dropSlot = hit ? hit.GetComponentInParent<Slot>() : null;
        Slot originalSlot = originalParent ? originalParent.GetComponent<Slot>() : null;

        if (dropSlot != null)
        {
            // If squeeze was triggered, the slot should already be empty
            if (squeezeTriggered)
            {
                // Clear original slot if dragging from a slot
                if (originalSlot != null)
                {
                    originalSlot.brickPrefab = null;
                }

                // Place the dragged brick in the now-empty slot
                dropSlot.brickPrefab = gameObject;
                transform.SetParent(dropSlot.transform, false);
                SnapUI(transform as RectTransform);
                return;
            }

            // MODIFIED: Destroy old brick instead of swapping
            GameObject oldBrick = dropSlot.brickPrefab;

            // If slot already had a brick, destroy it
            if (oldBrick != null)
            {
                Destroy(oldBrick);
            }

            // Clear the original slot if dragging from a slot
            if (originalSlot != null)
            {
                originalSlot.brickPrefab = null;
            }

            // Assign dragged brick to the drop slot
            dropSlot.brickPrefab = gameObject;

            // Re-parent dragged brick to the drop slot and center it
            transform.SetParent(dropSlot.transform, false);
            SnapUI(transform as RectTransform);

            return;
        }

        // delete the dragged item if dropped outside inventory panel
        if (originalSlot != null && originalSlot.brickPrefab == gameObject)
            originalSlot.brickPrefab = null;

        Destroy(gameObject);
    }

    // Ensure the UI element is centered inside its parent
    private void SnapUI(RectTransform rt)
    {
        if (rt == null) { transform.localPosition = Vector3.zero; return; }

        // Ignore any layout group from the previous parent
        var le = rt.GetComponent<LayoutElement>();
        if (le) le.ignoreLayout = true;

        // Center in parent
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    // Create a simple UI image we can stretch into a thin vertical line
    private void CreateInsertionMarker()
    {
        if (insertionMarker != null) return;

        insertionMarker = new GameObject("InsertionMarker");
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) insertionMarker.transform.SetParent(canvas.transform, false);

        var image = insertionMarker.AddComponent<Image>();
        image.raycastTarget = false;

        // Gray color
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        var rectTransform = insertionMarker.GetComponent<RectTransform>();
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // NOTE: no Outline component (no lines)
        insertionMarker.SetActive(false);
    }

    // Always draw a thin vertical line on the LEFT edge of the hovered slot
    private void ShowInsertionMarker(Slot slot)
    {
        if (insertionMarker == null || slot == null) return;

        insertionMarker.SetActive(true);

        var slotRT = slot.GetComponent<RectTransform>();
        var markerRT = insertionMarker.GetComponent<RectTransform>();
        if (slotRT == null || markerRT == null) return;

        // Get slot corners
        Vector3[] c = new Vector3[4];
        slotRT.GetWorldCorners(c);
        // c[1] = top-left, c[0] = bottom-left

        Vector3 top = c[1];
        Vector3 bottom = c[0];

        Vector3 mid = (top + bottom) * 0.5f;
        float height = Vector3.Distance(top, bottom) * 35f; // extend beyond slot height

        // Shift the line left so it sits on the slot border
        markerRT.position = mid + new Vector3(-0.1f, 0f, 0f);
        markerRT.sizeDelta = new Vector2(5f, height); // thin vertical line
        markerRT.localRotation = Quaternion.identity;
    }


    private void HideInsertionMarker()
    {
        if (insertionMarker != null) insertionMarker.SetActive(false);
    }
}
