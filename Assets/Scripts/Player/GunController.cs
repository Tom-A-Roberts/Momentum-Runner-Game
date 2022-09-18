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
    public Transform myCamera;
    public PlayerAudioManager audioManager;
    public PlayerNetworking playerNetworking;

    [Header("Gameplay Settings")]
    [Tooltip("How long in seconds between times you can shoot, checked on the clientside")]
    public float shootCooldownClientside = 0.5f;
    [Tooltip("How long in seconds between times you can shoot, checked by the server")]
    public float shootCooldownServerside = 0.45f;

    [Tooltip("Innacuracy of gun. 0 is perfect, 0.25 means 45-degrees of variation, 1 means 360-degrees.")]
    public float innacuracy = 0.01f;

    [Tooltip("How much heat each shot builds up")]
    public float heatPerShot = 0.1f;

    [Tooltip("Time before starting the cooling/reloading sequence")]
    public float heatReloadingWaitTime = 2f;

    [Tooltip("How quickly should a full cool take (in seconds)")]
    public float heatReloadCoolingSpeed = 3f;

    [Tooltip("What layers can be shot")]
    public LayerMask shootableLayers;


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
    public ParticleSystem steamParticles;
    public float glowIncreaseMultiplier = 1f;

    public HeatCoolingGunState myGunState;

    [System.NonSerialized]
    public bool spectatorMode = false;

    private Renderer myRend;
    private Material myMat;

    private Color emissiveColour;
    private float animationProgress = 1;
    private bool animationActive = false;
    private float clientsideCooldownProgress = 0;
    private Vector3 originalGunAngle;
    private Vector3 originalSlidePosition;

    private float hOriginal;
    private float sOriginal;
    private float vOriginal;


    public class HeatCoolingGunState
    {
        // How much heat each shot builds up
        public readonly float heatPerShot;

        // How long you must wait between shots (in serverside time)
        public readonly float shootCooldownTime;

        // Time before starting the cooling/reloading sequence
        public readonly float heatReloadingWaitTime;

        // How quickly should a full cool take (in seconds)
        public readonly float heatReloadCoolingSpeed;


        public HeatDisplayUI heatDisplay;
        public PlayerAudioManager audioManager;
        public ParticleSystem steamParticles;

        private float heatLevel = 0;
        private bool isOverheatedServerside;
        private bool isOverheatedClientside;
        private float waitTimeProgress;
        private float singleShotCooldownProgress;


        /// <summary>
        /// Tests whether you can shoot currently or not
        /// </summary>
        public bool CanShootServerside
        {
            get => (!isOverheatedServerside) && singleShotCooldownProgress == 0 && heatLevel != 1;
        }
        public bool CanShootClientside
        {
            get => (!isOverheatedClientside) && singleShotCooldownProgress == 0 && heatLevel != 1;
        }

        /// <param name="_heatPerShot">How much heat each shot builds up</param>
        /// <param name="_shootCooldownTime">How long you must wait between shots (in serverside time)</param>
        /// <param name="_heatReloadingWaitTime">Time before starting the cooling/reloading sequence</param>
        /// <param name="_heatReloadCoolingSpeed">How quickly should a full cool take (in seconds)</param>
        public HeatCoolingGunState(float _heatPerShot = 0.1f, float _shootCooldownTime = 0.45f, float _heatReloadingWaitTime = 0.5f, float _heatReloadCoolingSpeed = 1f)
        {
            heatPerShot = _heatPerShot;
            shootCooldownTime = _shootCooldownTime;
            heatReloadingWaitTime = _heatReloadingWaitTime;
            heatReloadCoolingSpeed = _heatReloadCoolingSpeed;
        }

        public void Update(float deltaTime)
        {
            if(waitTimeProgress > 0)
            {
                waitTimeProgress -= deltaTime / heatReloadingWaitTime;
                if (waitTimeProgress <= 0)
                {
                    ReloadHiss();
                    waitTimeProgress = 0;
                }
                    
            }
            if(waitTimeProgress == 0 && heatLevel > 0)
            {
                //if (heatLevel == 1)
                //    ReloadHiss();

                heatLevel -= deltaTime / heatReloadCoolingSpeed;
                if (heatLevel < 0)
                    heatLevel = 0;
                UpdateUIAndEffects();
            }

            if (waitTimeProgress == 0 && isOverheatedClientside && heatLevel == 0)
            {
                isOverheatedClientside = false;
                UpdateUIAndEffects();
            }
            if (waitTimeProgress == 0 && isOverheatedServerside && heatLevel < 0.2f)
            {
                isOverheatedServerside = false;
            }
            if (singleShotCooldownProgress > 0)
            {
                singleShotCooldownProgress -= deltaTime / shootCooldownTime;
                if (singleShotCooldownProgress < 0)
                    singleShotCooldownProgress = 0;
            }


        }
        public void Shoot()
        {
            if (CanShootServerside)
            {
                singleShotCooldownProgress = 1;
                waitTimeProgress = 1;
                heatLevel += heatPerShot;
                if(heatLevel >= 1)
                {
                    heatLevel = 1;

                    isOverheatedClientside = true;
                    isOverheatedServerside = true;
                    LongOverheat();
                }
                UpdateUIAndEffects();
            }
            else
            {
                Debug.LogWarning("Tried to shoot but couldn't shoot! Please check you can shoot before invoking this function");
            }
            
        }

        public void UpdateUIAndEffects()
        {
            if (heatDisplay)
            {
                heatDisplay.SetHeatLevel(heatLevel, isOverheatedClientside);
            }
            if (steamParticles)
            {
                var emission = steamParticles.emission;
                if(waitTimeProgress == 0 && heatLevel > 0)
                    emission.rateOverTime = 50 * heatLevel;
                else
                    emission.rateOverTime = 0;
            }
            else
            {

            }
        }

        public void LongOverheat()
        {
            if (audioManager)
                audioManager.OverheatedGun();
            // Play long overheat sound
        }
        public void ReloadHiss()
        {
            if (audioManager)
                audioManager.GunCoolingEffect(heatLevel);
            // Play cooling hiss sound
        }
    }

    private void Start()
    {
        myGunState = new HeatCoolingGunState(_heatPerShot: heatPerShot, _shootCooldownTime: shootCooldownServerside, _heatReloadingWaitTime: heatReloadingWaitTime, _heatReloadCoolingSpeed: heatReloadCoolingSpeed);
        if (playerNetworking.IsOwner)
            myGunState.heatDisplay = HeatDisplayUI.Instance;
        myGunState.audioManager = audioManager;
        myGunState.steamParticles = steamParticles;
        var emission = steamParticles.emission;
        emission.rateOverTime = 0;

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
        if(clientsideCooldownProgress > 0)
        {
            clientsideCooldownProgress -= Time.deltaTime / shootCooldownClientside;
            if(clientsideCooldownProgress < 0)
            {
                clientsideCooldownProgress = 0;
            }
        }

        myGunState.Update(Time.deltaTime);
        bool inFinishedGameState = GameStateManager.Singleton.GameState == GameState.winState || GameStateManager.Singleton.GameState == GameState.podium;
        bool canShoot = (myGunState.CanShootClientside && playerNetworking.IsOwner) || (myGunState.CanShootServerside && !playerNetworking.IsOwner);
        if (Input.GetButton("Shoot") && clientsideCooldownProgress <= 0 && canShoot && !IngameEscMenu.Singleton.curserUnlocked && !spectatorMode && !inFinishedGameState)
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

    public void DoShoot(Vector3 startPos, Vector3 shootDirection)
    {
        RaycastHit hitRaycastReferenceObj;
        bool hit;
        Vector3 shootDirectionAfterAimAssist = AdjustForAimAssist(startPos, shootDirection, out hit, out hitRaycastReferenceObj);

        AnimateShoot(shootDirectionAfterAimAssist, hit, hitRaycastReferenceObj);
    }

    public GameObject TryShoot(Vector3 startPos, Vector3 shootDirection)
    {
        if (myGunState.CanShootServerside)
        {
            myGunState.Shoot();

            RaycastHit hitRaycastReferenceObj;
            bool hit;            
            AdjustForAimAssist(startPos, shootDirection, out hit, out hitRaycastReferenceObj);

            if (hit)
            {
                GameObject hitGameObject = hitRaycastReferenceObj.collider.gameObject;

                return hitGameObject;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
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
        hitBool = Physics.Raycast(startPos, shootDirection, out hitRaycastReferenceObj, raycastDistance, shootableLayers);

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
                        bool targetHit = Physics.Raycast(startPos, shootDirectionToTarget, out targetRaycastHit, raycastDistance, shootableLayers);
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
    public void AnimateShoot(Vector3 shootDirection, bool hit, RaycastHit hitRaycastReferenceObj)
    {
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
            lr.SetPosition(1, hitRaycastReferenceObj.point);

            GameObject ground_particle = Instantiate(groundHitParticlePrefab, hitRaycastReferenceObj.point, Quaternion.FromToRotation(Vector3.forward, hitRaycastReferenceObj.normal));
            GameObject ground_decal = Instantiate(bulletHoleDecalPrefab, hitRaycastReferenceObj.point, Quaternion.FromToRotation(Vector3.forward, hitRaycastReferenceObj.normal));
        }
        else
        {
            lr.SetPosition(1, myCamera.position + shootDirection * raycastDistance);
        }
        audioManager.Shoot();
        GameObject muzzle_particle = Instantiate(muzzleFlashParticlePrefab, muzzlePoint.position, muzzlePoint.rotation);//
        muzzle_particle.transform.parent = muzzlePoint.transform;

        // Start gun animation
        animationActive = true;
        animationProgress = 0;
        clientsideCooldownProgress = 1;
    }

}
