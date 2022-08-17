using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
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

	private Vector3 cameraOffsetFromBody;
	private float originalFOV;
	private Camera myCamera;
	private PlayerController playerScript;
	private Rigidbody playerRigidBody;
    private void Start()
    {
		cameraOffsetFromBody = transform.position - body.transform.position;
		myCamera = GetComponent<Camera>();
		playerScript = body.GetComponent<PlayerController>();
		playerRigidBody = body.GetComponent<Rigidbody>();
		originalFOV = myCamera.fieldOfView;
		Cursor.lockState = CursorLockMode.Locked; //Lock mouse cursor to screen
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
		rotation.x += Input.GetAxis(xAxis) * sensitivity;
		rotation.y += Input.GetAxis(yAxis) * sensitivity;
		rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
		var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
		var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);

		transform.localRotation = xQuat * yQuat; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler. transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);

		UpdateCameraFOV();
	}

	void UpdateCameraFOV()
    {
		float walkSpeed = playerScript.WalkingSpeed;
		float sprintSpeed = walkSpeed * playerScript.SprintMultiplier;

		Vector3 planarVelocity = new Vector3(playerRigidBody.velocity.x, 0, playerRigidBody.velocity.z);
		float currentSpeed = planarVelocity.magnitude;

		float newFOV = originalFOV;


		if (currentSpeed < walkSpeed)
        {
			// We are walking or slower, so use original FOV with multiplier at 1
			newFOV = originalFOV * 1;
		}else if(currentSpeed < sprintSpeed)
        {
			// We are sprinting/accelerating to sprint, so use FOV sprint multiplier
			newFOV = originalFOV * Mathf.Lerp(1, FOVMultiperWhenSprinting, Mathf.Clamp01((currentSpeed - walkSpeed)/(sprintSpeed- walkSpeed)));

		}
		else if(currentSpeed < MaxFOVSpeed)
        {
			// We are going faster than sprinting, so use Max FOV Multiplier
			newFOV = originalFOV * Mathf.Lerp(FOVMultiperWhenSprinting, MaxFOVMultiplier, Mathf.Clamp01((currentSpeed - sprintSpeed) / (MaxFOVSpeed - sprintSpeed)));
		}
		else
        {
			newFOV = originalFOV * MaxFOVMultiplier;
		}
		myCamera.fieldOfView = newFOV;
	}

}
