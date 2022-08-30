using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowFlashObject : MonoBehaviour
{
    [Tooltip("The time it takes for the glow to reach full brightness")]
    public float glowIncreaseTime = 0.1f;
    [Tooltip("The time it takes for the glow to decay to the normal brightness")]
    public float glowDecaySpeed = 0.5f;
    [Tooltip("How much more glow there should be during interaction")]
    public float glowIncreaseMultiplier = 5f;
    [Tooltip("Hue change during interaction")]
    public float hueChange = 0.05f;

    private float glowState = 0;
    private bool increaseGlow = false;
    private Color emissiveColour;
    private Renderer myRend;
    private Material myMat;
    private GrappleGun grappleTracker;
    private WallRunning wallrunTracker;

    private float hOriginal;
    private float sOriginal;
    private float vOriginal;
    void Start()
    {
        myRend = GetComponent<Renderer>();
        foreach (Material material in myRend.materials)
        {
            Color testColour = material.GetColor("_EmissiveColor");
            if (testColour.r > 0 || testColour.g > 0 || testColour.b > 0)
            {
                emissiveColour = testColour;
                myMat = material;
            }
        }
        Color.RGBToHSV(emissiveColour, out hOriginal, out sOriginal, out vOriginal);
    }

    // Update is called once per frame
    void Update()
    {
        if(grappleTracker != null)
        {
            if (grappleTracker.IsGrappling())
            {
                IncreaseGlow();
            }
            else
            {
                grappleTracker = null;
            }
        }
        if (wallrunTracker != null)
        {
            if (wallrunTracker.IsWallRunning)
            {
                IncreaseGlow();
            }
            else
            {
                wallrunTracker = null;
            }
        }

        if (glowState > 0)
        {
            glowState -= Time.deltaTime / glowDecaySpeed;

            if (glowState < 0) glowState = 0;

            float glowMultiplier = 1 + glowState * glowIncreaseMultiplier;
            float currentHueChange = glowState * hueChange;

            if (!increaseGlow && myMat != null)
                myMat.SetColor("_EmissiveColor", Color.HSVToRGB(hOriginal + currentHueChange, sOriginal, vOriginal) * glowMultiplier);
            
        }
        if (increaseGlow)
        {
            glowState += Time.deltaTime / glowIncreaseTime;

            if (glowState > 1) glowState = 1;

            float glowMultiplier = 1 + glowState * glowIncreaseMultiplier;
            float currentHueChange = glowState * hueChange;

            if (myMat != null)
                myMat.SetColor("_EmissiveColor", Color.HSVToRGB(hOriginal + currentHueChange, sOriginal, vOriginal) * glowMultiplier);
        }
        increaseGlow = false;
    }

    public void GrappleStarted(GrappleGun referenceInitiator)
    {
        grappleTracker = referenceInitiator;
        IncreaseGlow();
    }
    public void WallRunStarted(WallRunning referenceInitiator)
    {
        wallrunTracker = referenceInitiator;
        IncreaseGlow();
    }
    public void IncreaseGlow()
    {
        increaseGlow = true;
    }
}
