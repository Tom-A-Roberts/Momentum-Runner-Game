using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingCamera : MonoBehaviour
{
    public static LoadingCamera Singleton { get; private set; }

    public Camera MyCamera => myCamera;
    private Camera myCamera;

    public bool IsActive => isActive;
    private bool isActive = true;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(Singleton);
        }
        Singleton = this;

        myCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    //void Update()
    //{
    //    if (isActive)
    //    {
    //        if (PlayerNetworking.localPlayer)
    //        {
    //            Disable();
    //        }
    //    }
    //}
    public void Enable()
    {
        myCamera.enabled = true;
        isActive = true;
    }
    public void Disable()
    {
        myCamera.enabled = false;
        isActive = false;
    }
}
