using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletFadeOut : MonoBehaviour
{
    // Start is called before the first frame update
    public float fadeOutSpeed = 0.2f;
    private float fadeOutProgress = 1f;
    private LineRenderer lr;
    private Color originalColour;
    void Start()
    {
        fadeOutProgress = 1f;
        lr = GetComponent<LineRenderer>();
        originalColour = lr.startColor;
    }

    // Update is called once per frame
    void Update()
    {
        fadeOutProgress -= Time.deltaTime / fadeOutSpeed;

        if(fadeOutProgress < 0)
        {
            Destroy(this.gameObject);
        }


        Color newColour = new Color(originalColour.r, originalColour.g, originalColour.b, originalColour.a - originalColour.a*(1- fadeOutProgress));

        lr.startColor = newColour;
        lr.endColor = newColour;

    }
}
