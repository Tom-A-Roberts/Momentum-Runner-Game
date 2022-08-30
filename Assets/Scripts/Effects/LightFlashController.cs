using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlashController : MonoBehaviour
{
    private Light myLight;
    public float flashDuration = 0.02f;
    private float flashProgress = 0;
    // Start is called before the first frame update
    void Start()
    {
        myLight = GetComponent<Light>();
        myLight.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(flashProgress > 0)
        {
            flashProgress -= Time.deltaTime / flashDuration;
            if(flashProgress <= 0)
            {
                flashProgress = 0;
                myLight.enabled = false;
            }
        }
    }

    public void Flash()
    {
        flashProgress = 1;
        myLight.enabled = true;
    }
}
