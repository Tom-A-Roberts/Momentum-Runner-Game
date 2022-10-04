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

    private bool temporarilyActivated = false;

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
    void Update()
    {
        //if(Camera.main == null || (Camera.main == myCamera && !isActive))
        //{
        //    Enable();
        //    Debug.Log("Null");
        //}
        //else
        //{
            
        //    if (Camera.main == myCamera && PlayerNetworking.localPlayer)
        //    {
        //        Disable();
        //        Debug.Log("Auto Disabling");
        //    }
        //    else
        //    {
        //        //Debug.Log(Camera.main.gameObject);
        //        //Debug.Log(Camera.allCameras.Length);
        //    }
        //}
        //if (!isActive && Camera.main == null && !temporarilyActivated)
        //{
        //    temporarilyActivated = true;
        //    Enable();
        //    //if (PlayerNetworking.localPlayer)
        //    //{
        //    //    Enable();
        //    //}
        //}

        //if (temporarilyActivated)
        //{

        //}
    }

    public void Enable()
    {
        myCamera.enabled = true;
        isActive = true;
        temporarilyActivated = false;
    }
    public void Disable()
    {
        myCamera.enabled = false;
        isActive = false;
        temporarilyActivated = false;
    }
}
