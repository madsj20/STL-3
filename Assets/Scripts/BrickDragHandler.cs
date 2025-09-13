using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BrickDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        // Parent til nærmeste Canvas, så den kan flyttes frit over UI
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) transform.SetParent(canvas.transform, true);

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

        RectTransform parentRt = rt.parent as RectTransform;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRt, eventData.position, eventData.pressEventCamera, out localPos);
        rt.anchoredPosition = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        var hit = eventData.pointerCurrentRaycast.gameObject;
        Slot dropSlot     = hit ? hit.GetComponentInParent<Slot>() : null;
        Slot originalSlot = originalParent ? originalParent.GetComponent<Slot>() : null;

        if (dropSlot != null)
        {
            // swap-bookkeeping
            GameObject droppedBrick = dropSlot.brickPrefab;

            dropSlot.brickPrefab = gameObject;

            if (droppedBrick != null && originalSlot != null)
            {
                originalSlot.brickPrefab = droppedBrick;
                droppedBrick.transform.SetParent(originalSlot.transform, false);
                SnapUI(droppedBrick.transform as RectTransform);
            }
            else if (originalSlot != null)
            {
                originalSlot.brickPrefab = null;
            }

            // flyt denne ind i slotten (adoptér lokalrum)
            transform.SetParent(dropSlot.transform, false);
            SnapUI(transform as RectTransform);
        }
        else
        {
            // ingen gyldig slot -> tilbage til original parent
            transform.SetParent(originalParent, false);
            SnapUI(transform as RectTransform);
        }
    }

    private void SnapUI(RectTransform rt)
    {
        if (rt == null) { transform.localPosition = Vector3.zero; return; }

        // ignorér layout fra tidligere parent
        var le = rt.GetComponent<LayoutElement>();
        if (le) le.ignoreLayout = true;

        // centér i parent
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale    = Vector3.one;
    }
}
