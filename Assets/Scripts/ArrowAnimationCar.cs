using UnityEngine;

public class ArrowAnimationCar : MonoBehaviour
{
    public SpriteRenderer[] arrows;
    public float cycleTime = 0.6f;

    void Update()
    {
        float t = Time.time % cycleTime;
        float segment = cycleTime / arrows.Length;

        for (int i = 0; i < arrows.Length; i++)
        {
            // calculate alpha based on time segment
            float start = i * segment;
            float end = start + segment;

            float alpha = (t >= start && t < end) ? 1f : 0.2f; // active arrow is fully visible, others are dimmed
            Color c = arrows[i].color;
            arrows[i].color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}