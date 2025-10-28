using TMPro;
using UnityEngine;

public class FinishTime : MonoBehaviour
{

    [SerializeField] private RaceTimer timer; // Reference to the RaceTimer in this scene
    [SerializeField] private TMP_Text FinishTimeText;


    private void Awake()
    {
        if (!timer) timer = FindFirstObjectByType<RaceTimer>();

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!FinishTimeText) FinishTimeText = gameObject.GetComponent<TMP_Text>();
        if (FinishTimeText != null)
        {
            FinishTimeText.text = timer.GetFormattedTime();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
