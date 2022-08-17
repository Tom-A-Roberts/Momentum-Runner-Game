using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [Header("Related Objects")]
    public Transform gunTop;
    public Transform muzzlePoint;


    [Header("Gameplay Settings")]
    [Tooltip("How long in seconds between times you can shoot")]
    public float shootCooldown = 0.5f;
    [Tooltip("Innacuracy of gun. 0 is perfect, 0.25 means 45-degrees of variation, 1 means 360-degrees.")]
    public float innacuracy = 0.01f;

    [Header("Animation Settings")]
    [Tooltip("How long in seconds the shooting animation will last")]
    public float animationDuration = 0.8f;
    [Tooltip("How far back (in degrees) each shot will kick the gun back in your hand")]
    public float kickbackAngle = 20f;
    [Tooltip("How far back (in spacial units) each shot will slide the sliding element of the gun back")]
    public float slidebackDistance = 0.16f;

    [Header("Effects Settings")]
    public Color lineColor;
    public Shader lineShader;
    public GameObject groundHitParticlePrefab;
    public GameObject muzzleFlashParticlePrefab;

    private float animationProgress = 1;
    private bool animationActive = false;
    private float cooldownProgress = 0;
    private Vector3 originalGunAngle;
    private Vector3 originalSlidePosition;

    private void Start()
    {
        originalGunAngle = transform.localEulerAngles;
        originalSlidePosition = gunTop.localPosition;
        animationProgress = 1;
        SetGunTransformBasedOnProgress(animationProgress);
    }

    void Update()
    {
        if(animationActive)
        {
            animationProgress += Time.deltaTime / animationDuration;
            if (animationProgress > 1)
            {
                animationProgress = 1;
                animationActive = false;
            }
            SetGunTransformBasedOnProgress(animationProgress);
        }
        if(cooldownProgress > 0)
        {
            cooldownProgress -= Time.deltaTime / shootCooldown;
            if(cooldownProgress < 0)
            {
                cooldownProgress = 0;
            }
        }
        
        if (Input.GetMouseButton(1) && cooldownProgress <= 0)
        {
            animationActive = true;
            animationProgress = 0;
            cooldownProgress = 1;
            Shoot();
        }
    }

    void SetGunTransformBasedOnProgress(float progress)
    {
        float angleresult = ((Mathf.Pow(progress - 1, 2) + Mathf.Pow(progress - 1, 51))) / 0.8418f;
        float halfProgress = Mathf.Clamp01(progress * 3f);
        float slidebackResult = 0;
        if (halfProgress < 1)
        {
            slidebackResult = ((Mathf.Pow(halfProgress - 1, 2) + Mathf.Pow(halfProgress - 1, 51))) / 0.8418f;
        }

        //float slidebackResult = ((Mathf.Pow(progress - 1, 2) + Mathf.Pow(progress - 1, 15))) / 0.6357f;
        transform.localEulerAngles = new Vector3(originalGunAngle.x - angleresult * kickbackAngle, originalGunAngle.y, originalGunAngle.z);
        gunTop.localPosition = new Vector3(originalSlidePosition.x, originalSlidePosition.y, originalSlidePosition.z - slidebackDistance* slidebackResult);
    }

    void Shoot()
    {
        // Add random spread
        var shootDirection = Vector3.Slerp(Camera.main.transform.forward, Random.onUnitSphere, innacuracy);
        RaycastHit hitObj;
        bool hit = Physics.Raycast(Camera.main.transform.position + Camera.main.transform.forward, shootDirection, out hitObj, 100.0f);

        GameObject myLine = null;

        myLine = new GameObject();
        myLine.transform.position = muzzlePoint.position;
        myLine.AddComponent<LineRenderer>();
        myLine.AddComponent<BulletFadeOut>();

        var lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(lineShader);

        lr.startColor = lineColor;
        lr.endColor = lineColor;

        for (int i = 0; i < lr.colorGradient.alphaKeys.Length; i++)
        {
            lr.colorGradient.alphaKeys[i].alpha = 0.5f;
        }
        lr.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));


        lr.startWidth = 0.003f;
        lr.endWidth = 0.03f;
        lr.SetPosition(0, muzzlePoint.position);
        if (hit)
        {

            lr.SetPosition(1, hitObj.point);


            GameObject ground_particle = Instantiate(groundHitParticlePrefab, hitObj.point, Quaternion.FromToRotation(Vector3.forward, hitObj.normal));
            
        }
        else
        {
            lr.SetPosition(1, Camera.main.transform.position + shootDirection * 100f);
        }
        GameObject muzzle_particle = Instantiate(muzzleFlashParticlePrefab, muzzlePoint.position, muzzlePoint.rotation);//
        muzzle_particle.transform.parent = muzzlePoint.transform;
    }
}