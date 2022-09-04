using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GunController : MonoBehaviour
{
    [Header("Related Objects")]
    public GameObject gunModel;
    public Transform gunTop;
    public Transform muzzlePoint;
    public PlayerAudioManager audioManager;
    public PlayerNetworking playerNetworking;

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
    public Material lineMat;
    public GameObject groundHitParticlePrefab;
    public GameObject muzzleFlashParticlePrefab;
    public GameObject bulletHoleDecalPrefab;
    public LightFlashController muzzleLight;
    public float glowIncreaseMultiplier = 1f;

    private Renderer myRend;
    private Material myMat;

    private Color emissiveColour;
    private float animationProgress = 1;
    private bool animationActive = false;
    private float cooldownProgress = 0;
    private Vector3 originalGunAngle;
    private Vector3 originalSlidePosition;

    private float hOriginal;
    private float sOriginal;
    private float vOriginal;

    private void Start()
    {
        originalGunAngle = gunModel.transform.localEulerAngles;
        originalSlidePosition = gunTop.localPosition;
        animationProgress = 1;
        SetGunTransformBasedOnProgress(animationProgress);

        myRend = gunModel.GetComponent<Renderer>();
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
        
        if (Input.GetButton("Shoot") && cooldownProgress <= 0)
        {
            var shootDirection = Vector3.Slerp(Camera.main.transform.forward, Random.onUnitSphere, innacuracy);
            var shootStart = Camera.main.transform.position + Camera.main.transform.forward;

            playerNetworking.ShootStart(shootStart, shootDirection);
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

        gunModel.transform.localEulerAngles = new Vector3(originalGunAngle.x - angleresult * kickbackAngle, originalGunAngle.y, originalGunAngle.z);
        gunTop.localPosition = new Vector3(originalSlidePosition.x, originalSlidePosition.y, originalSlidePosition.z - slidebackDistance* slidebackResult);

        float glowMultiplier = 1 + (1-progress) * glowIncreaseMultiplier;
        float currentHueChange = (1-progress) * -0.025f;
        if (myMat != null)
            myMat.SetColor("_EmissiveColor", Color.HSVToRGB(hOriginal + currentHueChange, sOriginal, vOriginal) * glowMultiplier);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="shootDirection"></param>
    /// <returns>The gameobject that was shot. Is null if nothing is shot</returns>
    public GameObject Shoot(Vector3 startPos, Vector3 shootDirection)
    {
        // Add random spread
        RaycastHit hitRaycastReferenceObj;
        GameObject hitGameObject = null;
        bool hit = Physics.Raycast(startPos, shootDirection, out hitRaycastReferenceObj, 100.0f);

        GameObject myLine = new GameObject();
        myLine.transform.position = muzzlePoint.position;
        myLine.AddComponent<LineRenderer>();
        myLine.AddComponent<BulletFadeOut>();

        var lr = myLine.GetComponent<LineRenderer>();
        lr.material = lineMat;

        lr.startColor = lineColor;
        lr.endColor = lineColor;

        for (int i = 0; i < lr.colorGradient.alphaKeys.Length; i++)
        {
            lr.colorGradient.alphaKeys[i].alpha = 0.5f;
        }
        lr.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));

        lr.startWidth = 0.001f;
        lr.endWidth = 0.03f;
        lr.SetPosition(0, muzzlePoint.position);
        if (hit)
        {
            Target hitObject = hitRaycastReferenceObj.transform.gameObject.GetComponent<Target>();
            if(!(hitObject == null))
            {
                hitObject.TargetHit();
            }

            hitGameObject = hitRaycastReferenceObj.collider.gameObject;

            lr.SetPosition(1, hitRaycastReferenceObj.point);

            GameObject ground_particle = Instantiate(groundHitParticlePrefab, hitRaycastReferenceObj.point, Quaternion.FromToRotation(Vector3.forward, hitRaycastReferenceObj.normal));
            GameObject ground_decal = Instantiate(bulletHoleDecalPrefab, hitRaycastReferenceObj.point, Quaternion.FromToRotation(Vector3.forward, hitRaycastReferenceObj.normal));
        }
        else
        {
            lr.SetPosition(1, Camera.main.transform.position + shootDirection * 100f);
        }
        audioManager.Shoot();
        GameObject muzzle_particle = Instantiate(muzzleFlashParticlePrefab, muzzlePoint.position, muzzlePoint.rotation);//
        muzzle_particle.transform.parent = muzzlePoint.transform;

        // Start gun animation
        animationActive = true;
        animationProgress = 0;
        cooldownProgress = 1;

        return hitGameObject;
    }
}