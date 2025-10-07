using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TrackSelectManager : MonoBehaviour
{
    [Header("Track Data")]
    public List<TrackData> tracks;

    [Header("UI References")]
    public Transform contentRoot;      // ScrollView -> Viewport -> Content
    public TrackItemUI itemPrefab;     // Prefab for the scroll view thumbnails
    public Image previewImage;         // Big image above scroll area
    public Button playButton;          // Play button

    private readonly List<TrackItemUI> spawnedItems = new();
    private int selectedIndex = -1;

    private void Start()
    {
        BuildTrackList();
        if (tracks.Count > 0)
            SelectTrack(0);

        playButton.onClick.AddListener(OnPlayPressed);
    }

    private void BuildTrackList()
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        spawnedItems.Clear();

        for (int i = 0; i < tracks.Count; i++)
        {
            var data = tracks[i];
            var item = Instantiate(itemPrefab, contentRoot);
            item.SetImage(data.trackImage);
            item.SetSelected(false);

            int capturedIndex = i;
            item.Button.onClick.AddListener(() => SelectTrack(capturedIndex));

            spawnedItems.Add(item);
        }
    }

    private void SelectTrack(int index)
    {
        if (index < 0 || index >= tracks.Count) return;

        selectedIndex = index;
        previewImage.sprite = tracks[index].trackImage;

        for (int i = 0; i < spawnedItems.Count; i++)
            spawnedItems[i].SetSelected(i == index);
    }

    private void OnPlayPressed()
    {
        if (selectedIndex < 0 || selectedIndex >= tracks.Count) return;

        var sceneName = tracks[selectedIndex].sceneName;
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("Track scene name is empty!");
    }
}
