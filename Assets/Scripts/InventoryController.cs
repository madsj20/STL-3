using UnityEngine;

public class InventoryController : MonoBehaviour
{

    public GameObject inventoryPanel; //The panel that holds the inventory slots 
    public Slot slotPrefab; //Array of all the slots in the inventory
    public int slotCount; //Index of the currently selected slot
    public GameObject[] brickPrefabs; //Array of all the brick prefabs available in the game
    public GameObject pickablePanel;  // <-- assign your left panel here


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
}
