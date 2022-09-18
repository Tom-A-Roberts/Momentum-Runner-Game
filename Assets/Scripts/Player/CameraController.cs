using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Camera))]
public class CameraController : NetworkBehaviour
{
    public GameObject body;

	[Header("Dynamic FOV Settings")]
	//[Tooltip("The power of the deceleration that stops the player sliding sideways")]
	[Tooltip("When sprinting, FOV gets to FOVMultiperWhenSprinting")]
	public float FOVMultiperWhenSprinting = 1.1f;
	[Tooltip("When at max FOV speed, FOV gets to MaxFOVMultiplier")]
	public float MaxFOVMultiplier = 1.4f;
	[Tooltip("How fast you have to go in order to reach maximum FOV")]
	public float MaxFOVSpeed = 20f;

	[Range(0f, 0.1f)]
	public float fovChangeSmoothing = 0.1f;

	[System.NonSerialized]
	public float zRotation = 0;

    [System.NonSerialized]
    public bool spectatorMode = false;

    private float originalFOV;
	private float targetFOV;
	private float FOVchangeCurrentVelocity = 0.0f;
	private Camera myCamera;
	private PlayerController playerScript;
	private Rigidbody playerRigidBody;
    private void Start()
    {
		myCamera = GetComponent<Camera>();
		playerScript = body.GetComponent<PlayerController>();
		playerRigidBody = body.GetComponent<Rigidbody>();
		originalFOV = myCamera.fieldOfView;
		
	}

    // Code thanks to: https://gist.github.com/KarlRamstedt/407d50725c7b6abeaf43aee802fdd88e
    public float Sensitivity
	{
		get { return sensitivity; }
		set { sensitivity = value; }
	}
	[Range(0.1f, 9f)][SerializeField] float sensitivity = 2f;
	[Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
	[Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;

	Vector2 rotation = Vector2.zero;
	const string xAxis = "Mouse X"; //Strings in direct code generate garbage, storing and re-using them creates no garbage
	const string yAxis = "Mouse Y";

	void Update()
	{

		bool inFinishedGameState = GameStateManager.Singleton.GameState == GameState.winState || GameStateManager.Singleton.GameState == GameState.podium;

        if (!IngameEscMenu.Singleton.curserUnlocked && !inFinishedGameState)
        {
			rotation.x += Input.GetAxis(xAxis) * sensitivity;
			rotation.y += Input.GetAxis(yAxis) * sensitivity;
		}
		rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
		var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
		var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);

		
		var zQuat = Quaternion.AngleAxis(zRotation, Vector3.forward);

		transform.localRotation = xQuat * yQuat * zQuat; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler. transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);

		UpdateCameraFOV();
	}

	void UpdateCameraFOV()
    {
		float walkSpeed = playerScript.WalkingSpeed;
		float sprintSpeed = walkSpeed * playerScript.SprintMultiplier;

		Vector3 planarVelocity = new Vector3(playerRigidBody.velocity.x, 0, playerRigidBody.velocity.z);
		float currentSpeed = planarVelocity.magnitude;

		if (currentSpeed < walkSpeed)
        {
			// We are walking or slower, so use original FOV with multiplier at 1
			// Tom is a silly spogert
			targetFOV = originalFOV * 1;
		}else if(currentSpeed < sprintSpeed)
        {
			// We are sprinting/accelerating to sprint, so use FOV sprint multiplier
			targetFOV = originalFOV * Mathf.Lerp(1, FOVMultiperWhenSprinting, Mathf.Clamp01((currentSpeed - walkSpeed)/(sprintSpeed- walkSpeed)));

		}
		else if(currentSpeed < MaxFOVSpeed)
        {
			// We are going faster than sprinting, so use Max FOV Multiplier
			targetFOV = originalFOV * Mathf.Lerp(FOVMultiperWhenSprinting, MaxFOVMultiplier, Mathf.Clamp01((currentSpeed - sprintSpeed) / (MaxFOVSpeed - sprintSpeed)));
		}
		else
        {
			targetFOV = originalFOV * MaxFOVMultiplier;
		}

		if (!spectatorMode)
		{
            myCamera.fieldOfView = Mathf.SmoothDamp(myCamera.fieldOfView, targetFOV, ref FOVchangeCurrentVelocity, fovChangeSmoothing);
        }
		else
		{
			myCamera.fieldOfView = originalFOV;
        }
	}
}
