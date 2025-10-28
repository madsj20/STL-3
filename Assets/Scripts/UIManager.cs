using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject WinningUI;
    public static UIManager Instance { get; private set; }


    void Awake()
    {
        Instance = this;
    }
}