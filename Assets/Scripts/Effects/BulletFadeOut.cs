using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletFadeOut : MonoBehaviour
{
    // Start is called before the first frame update
    public float fadeOutSpeed = 0.4f;
    private float fadeOutProgress = 1f;
    private LineRenderer lr;
    private Color originalColour;

    private float originalStartSize;
    private float originalEndSize;
    void Start()
    {
        fadeOutProgress = 1f;
        lr = GetComponent<LineRenderer>();
        originalColour = lr.startColor;

        originalStartSize = lr.startWidth;
        originalEndSize = lr.endWidth;
    }

    // Update is called once per frame
    void Update()
    {
        fadeOutProgress -= Time.deltaTime / fadeOutSpeed;

        if(fadeOutProgress < 0)
        {
            Destroy(this.gameObject);
        }
        lr.startWidth = (fadeOutProgress) * originalStartSize;
        lr.endWidth = (fadeOutProgress) * originalEndSize;

        //Color newColour = new Color(originalColour.r * (1 - fadeOutProgress), originalColour.g * (1 - fadeOutProgress), originalColour.b * (1 - fadeOutProgress), originalColour.a - originalColour.a*(1- fadeOutProgress));

        //lr.startColor = newColour;
        //lr.endColor = newColour;

    }
}
