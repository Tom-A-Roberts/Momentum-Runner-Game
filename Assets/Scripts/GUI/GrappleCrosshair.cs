using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrappleCrosshair : MonoBehaviour
{
    public static GrappleCrosshair Instance { get; private set; }

    [System.NonSerialized]
    public bool spectatorMode = false;

    private Image myImage;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
    }
    void Start()
    {
        // Initialize to empty point
        UpdateGrappleLocation(GrappleGun.GrappleablePointInfo.Empty);
        myImage = GetComponent<Image>();
        myImage.enabled = false;
    }
    public void UpdateGrappleLocation(GrappleGun.GrappleablePointInfo pointInfo)
    {
        if(pointInfo.targetFound && Camera.main && !spectatorMode)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(pointInfo.grapplePoint);
            this.gameObject.transform.position = screenPos;
            if(myImage)
                myImage.enabled = true;
        }
        else
        {
            if (myImage)
                myImage.enabled = false;
        }
    }

}
