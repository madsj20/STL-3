using UnityEngine;
using UnityEngine.UI;

public class TrackItemUI : MonoBehaviour
{
    [SerializeField] private Image trackImage;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectionHighlight;

    public Button Button => button;

    public void SetImage(Sprite sprite)
    {
        trackImage.sprite = sprite;
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
    }
}
