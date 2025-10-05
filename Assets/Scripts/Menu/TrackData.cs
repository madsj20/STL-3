using UnityEngine;

[CreateAssetMenu(menuName = "Menu/Track Data", fileName = "TrackData")]
public class TrackData : ScriptableObject
{
    [Header("Track Info")]
    public Sprite trackImage;       // same image for thumbnail and preview
    public string sceneName;        // must match a scene in Build Settings
}
