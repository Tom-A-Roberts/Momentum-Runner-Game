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

    [Header("Aim Assist Settings")]
    [Tooltip("Aiming within this amount of degrees to an enemy will snap your aim to them")]
    public float aimAssistAngle = 4;
    [Tooltip("The distance at which you get most aim assist")]
    public float optimalAimAssistDistance = 15f;
    [Tooltip("The distance at which you stop getting any aim assist")]
    public float maximumAimAssistDistance = 50f;
    [Tooltip("The furthest bullet distance you can shoot before the rays no longer hit.")]
    public float raycastDistance = 100f;

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
    /// Loops through the players in the scene and checks whether they are within a certain angle of the shooting direction.
    /// If so, then the shot ray snaps to the player.
    /// </summary>
    /// <param name="startPos">Start position of raycast</param>
    /// <param name="shootDirection">Original direction of raycast</param>
    /// <param name="hitBool">Returns whether the shot hit anything or not (including with aimassist)</param>
    /// <param name="hitRaycastReferenceObj">Returns the racast hit information of the chosen ray, if there is any</param>
    /// <returns></returns>
    private Vector3 AdjustForAimAssist(Vector3 startPos, Vector3 shootDirection, out bool hitBool, out RaycastHit hitRaycastReferenceObj)
    {
        hitBool = Physics.Raycast(startPos, shootDirection, out hitRaycastReferenceObj, raycastDistance);

        Vector3 outShootDirection = shootDirection;
        // If the player missed, see if aim assist will help
        if (!hitBool || (hitBool && hitRaycastReferenceObj.collider.gameObject.tag != "Player"))
        {
            // Setup the variables to write to
            float closestAngle = float.MaxValue;
            Vector3 closestShootDirection = Vector3.zero;
            bool foundTarget = false;

            // Find the list of player gameobjects that are currently in the game
            Dictionary<ulong, GameObject>.ValueCollection inGameTargets = PlayerNetworking.ConnectedPlayers.Values;
            
            // Loop through the "targets", aka the other players in the game
            foreach (GameObject target in inGameTargets)
            {
                // Find the rigidbody attatched to the target in the most efficient way possible
                PlayerNetworking targetPlayerNetworking;
                if(target && target.TryGetComponent<PlayerNetworking>(out targetPlayerNetworking))
                {
                    Vector3 targetPos = targetPlayerNetworking.bodyRigidbody.position;

                    // Get direction to the target person
                    Vector3 shootDirectionToTarget = (targetPos - startPos).normalized;
                    // Check angle is within aim assist angle
                    float angle = Vector3.Angle(shootDirection, shootDirectionToTarget);
                    float distance = Vector3.Distance(startPos, targetPos);
                    float angleMultiplier = 1;

                    // Increase the aim assist angle as distance increases towards the optimal distance
                    if (distance < optimalAimAssistDistance)
                    {
                        angleMultiplier = Mathf.Clamp01(distance / optimalAimAssistDistance);
                        if (angleMultiplier < 0.5f)
                            angleMultiplier = 0.5f;
                    }

                    // Linearly decrease the aim assist angle after the optimal distance
                    if (distance > optimalAimAssistDistance)
                        angleMultiplier = 1 - Mathf.Clamp01((distance - optimalAimAssistDistance) / (maximumAimAssistDistance - optimalAimAssistDistance));

                    if (angle < aimAssistAngle * angleMultiplier)
                    {
                        // If within angle, see if raycast would hit
                        RaycastHit targetRaycastHit;
                        bool targetHit = Physics.Raycast(startPos, shootDirectionToTarget, out targetRaycastHit, raycastDistance);
                        if (targetHit)
                        {
                            // If target is hittable, see if it's the closest one to the mouse pointer
                            foundTarget = true;
                            if (angle < closestAngle)
                            {
                                closestAngle = angle;
                                closestShootDirection = shootDirectionToTarget;
                                hitRaycastReferenceObj = targetRaycastHit;
                            }
                        }
                    }
                }
            }
            if (foundTarget)
            {
                outShootDirection = closestShootDirection;
                hitBool = true;
            }
        }

        return outShootDirection;
    }

    /// <summary>
    /// Animates shooting the gun from a particlar start position and direction
    /// </summary>
    /// <returns>The gameobject that was shot. Is null if nothing is shot</returns>
    public GameObject Shoot(Vector3 startPos, Vector3 shootDirection)
    {

        GameObject hitGameObject = null;
        RaycastHit hitRaycastReferenceObj;
        bool hit;

        Vector3 shootDirectionAfterAimAssist = AdjustForAimAssist(startPos, shootDirection, out hit, out hitRaycastReferenceObj);

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
            lr.SetPosition(1, Camera.main.transform.position + shootDirectionAfterAimAssist * raycastDistance);
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
