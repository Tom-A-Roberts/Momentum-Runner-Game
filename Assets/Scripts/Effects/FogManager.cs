using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FogManager : MonoBehaviour
{
    public static FogManager Instance;

    // known objects
    public GameObject FogSpherePrefab;
    [System.NonSerialized]
    public GameObject playerBody;
    [System.NonSerialized]
    public Camera playerCamera;
    public GameObject sobelBarrier;
    public GameObject fogwallPlane;
    public GameObject sobelOverrideVolume;
    public GameObject fogParticles;

    public float DistanceToFogWall => distanceToFogWall;

    // privates
    private bool instantiated = false;
    private GameObject instantiatedSphericalFog;
    private Renderer sphericalFogRenderer;
    private MeshRenderer sphericalFogMeshRenderer;
    //private Volume sobelOverriderComponent;

    private float currentSphericalFogDistance = 200f;
    private float distanceToFogWall;
    private float fogWallDepth;

    //[System.NonSerialized]
    //public Sobel sobelRenderer;
    //private float sobelStartIntensity;
    //private float sobelStartMaxDistance;

    private void Awake()
    {
        //if (Instance != null && Instance != this)
        //{
        //    Destroy(Instance);
        //}
        //Instance = this;
    }
    // Start is called before the first frame update
    public void Initialize()
    {
        instantiatedSphericalFog = Instantiate(FogSpherePrefab, playerBody.transform.position, Quaternion.identity);
        sphericalFogRenderer = instantiatedSphericalFog.GetComponent<Renderer>(); 
        sphericalFogMeshRenderer = instantiatedSphericalFog.GetComponent<MeshRenderer>();
        //sobelBarrier.GetComponent<MeshRenderer>().enabled = true;
        //fogwallPlane.GetComponent<MeshRenderer>().enabled = true;
        //sobelOverrideVolume.GetComponent<BoxCollider>().enabled = true;
        //sobelOverrideVolume.GetComponent<Volume>().enabled = true;
        //sobelOverriderComponent = sobelOverrideVolume.GetComponent<Volume>();
        //fogWallDepth = fogwallPlane.GetComponent<Renderer>().material.GetFloat("_Distance");
        SetSphericalFogDepth(fogWallDepth);
        SetSphericalFogDistance(200f);

        instantiated = true;

    }

    public void ResetFog()
    {
        instantiated = false;

        Destroy(instantiatedSphericalFog);
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerBody)
        {
            ResetFog();
        }

        if (instantiated)
        {
            transform.position += transform.forward * 10 * Time.deltaTime;

            fogwallPlane.transform.position = transform.position;
            sobelOverrideVolume.transform.position = transform.position;


            

            instantiatedSphericalFog.transform.position = playerBody.transform.position;

            distanceToFogWall = Vector3.Dot(transform.forward, transform.position - playerBody.transform.position);

            UpdateSphericalFog();
            UpdateSobelBarrier();
            UpdateFogParticles();
            //UpdateSobel();
            //SetFogDistance(Vector3.Distance(playerBody.transform.position, transform.position));
        }
    }

    void UpdateFogParticles()
    {

        float characterOffset = -distanceToFogWall;
        if (characterOffset < 0)
            characterOffset = 0;

        Vector3 newPos = transform.position + transform.forward * (characterOffset -1);
        //newPos.y = playerBody.transform.position.y;
        fogParticles.transform.position = newPos;

    }

    void UpdateSobel()
    {
        float clampedDistanceToFogwall = distanceToFogWall;
        if (clampedDistanceToFogwall < 0)
            clampedDistanceToFogwall = 0;
        clampedDistanceToFogwall = Mathf.Clamp(clampedDistanceToFogwall - 10, 0, 50) / 50;
        float lerpOut = Mathf.Lerp(1, 0, clampedDistanceToFogwall);

        //sobelOverriderComponent.weight = lerpOut;


        //Debug.Log(lerpOut);
        ////sobelRenderer.fadeOffset = new MinFloatParameter(1f, 0f, true);
        //VolumeParameter<float> fr = new VolumeParameter<float>();
        //fr.value = lerpOut;
        //sobelRenderer.fadeOffset.overrideState = true;// = new MinFloatParameter(1f, lerpOut, true);
        //sobelRenderer.fadeOffset.value = lerpOut;// = new MinFloatParameter(1f, lerpOut, true);
        ////sobelRenderer.fadeOffset.SetValue(fr);// = new MinFloatParameter(1f, lerpOut, true);
    }
    

    void UpdateSphericalFog()
    {
        float clampedDistanceToFogwall = distanceToFogWall;
        if (clampedDistanceToFogwall < 0)
            clampedDistanceToFogwall = 0;
        const float sphericalFogwallClosingStartDistance = 5f;
        const float sphericalFogwallStartDepth = 80f;
        const float smalledSphereSize = 1;

        float sphericalDist01 = Mathf.Clamp(clampedDistanceToFogwall, 0, sphericalFogwallClosingStartDistance) / sphericalFogwallClosingStartDistance;
        float sphericalDistanceRemapped = Mathf.Clamp(sphericalDist01 * sphericalFogwallStartDepth, smalledSphereSize, sphericalFogwallStartDepth);
        if (sphericalDist01 == 1)
        {
            sphericalFogMeshRenderer.enabled = false;
        }
        else
        {
            sphericalFogMeshRenderer.enabled = true;
        }
        SetSphericalFogDistance(sphericalDistanceRemapped);
    }

    void UpdateSobelBarrier()
    {
        float characterOffset = -distanceToFogWall;
        if (characterOffset < 0)
            characterOffset = 0;
        sobelBarrier.transform.position = transform.position + transform.forward * (fogWallDepth * 2 + characterOffset + 10);
    }

    void SetSphericalFogDepth(float newDepth)
    {
        sphericalFogRenderer.material.SetFloat("_Distance", newDepth);
    }
    void SetSphericalFogDistance(float distance)
    {
        instantiatedSphericalFog.transform.localScale = new Vector3(distance * 2, distance * 2, distance * 2);
        currentSphericalFogDistance = distance;
    }
}
