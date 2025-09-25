using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BrickDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;
    int originalSiblingIndex;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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

        // Re-parent to the nearest Canvas so the brick can be dragged across all UI
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) transform.SetParent(canvas.transform, true);

        // Make the brick semi-transparent and allow raycasts to pass through
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        var rt = GetComponent<RectTransform>();
        if (!rt) { transform.position = eventData.position; return; }

        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out worldPos);
        rt.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
            // If a slot was hit, handle swapping logic
            GameObject droppedBrick = dropSlot.brickPrefab;

            // Assign dragged brick to the drop slot
            dropSlot.brickPrefab = gameObject;

            // If slot already had a brick, move it back to the original slot
            if (droppedBrick != null && originalSlot != null)
            {
                originalSlot.brickPrefab = droppedBrick;
                droppedBrick.transform.SetParent(originalSlot.transform, false);
                SnapUI(droppedBrick.transform as RectTransform);
            }
            else if (originalSlot != null)
            {
                // Otherwise, clear the original slot
                originalSlot.brickPrefab = null;
            }

            // Re-parent dragged brick to the drop slot and center it
            transform.SetParent(dropSlot.transform, false);
            SnapUI(transform as RectTransform);
            return;
        }

        // delete the dragged item if dropped outside inventory panel
        if (originalSlot != null && originalSlot.brickPrefab == gameObject)
            originalSlot.brickPrefab = null;

        Destroy(gameObject);
        /*
        else
        {
            // If no valid slot was hit, return to the original parent
            transform.SetParent(originalParent, false);
            SnapUI(transform as RectTransform);
        }
        */
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
}
