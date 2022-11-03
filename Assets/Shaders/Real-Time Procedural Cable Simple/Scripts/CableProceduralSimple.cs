using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[ExecuteAlways]
public class CableProceduralSimple : MonoBehaviour
{

	LineRenderer line;

	// The Start of the cable will be the transform of the Gameobject that has this component.
	// The Transform of the Gameobject where the End of the cable is. This needs to be assigned in the inspector.
	[SerializeField] Transform endPointTransform;

	// Number of points per meter
	//[SerializeField, Tooltip("Number of points per unit length, using the straight line from the start to the end transform.")] float pointDensity = 3;
	[SerializeField, Tooltip("Number of points in the liner renderer")] int numberOfPoints = 8;

	// How much the cable will sag by.
	[SerializeField] float sagAmplitude = 1;

	// How much wind will move the cable.
	[SerializeField] float swayMultiplier = 1;
	[SerializeField] float swayXMultiplier = 1;
	[SerializeField] float swayYMultiplier = .5f;
	// How fast the cable will go back and forth per second.
	[SerializeField] float swayFrequency = 1;

	[SerializeField] bool swayEnabled = false;

	[SerializeField] bool isPlyonCable = true;

	// These are used later for calculations
	int pointsInLineRenderer;
	Vector3 vectorFromStartToEnd;
	Vector3 sagDirection;
	float swayValue;

	private Vector3 myStart;
	private Vector3 myEnd;
	private Vector3 myEndScale;
	private Quaternion myEndRot;

	void UpdateEnds()
    {
		// Get direction Vector.


		Vector3 activeEndPoint = endPointTransform.position;

        if (isPlyonCable)
        {
			vectorFromStartToEnd = activeEndPoint - transform.position;
			Vector3 poleCenter = transform.parent.transform.position;
			float poleHeightDifference = (transform.position.y - poleCenter.y) - 12.5f;
			//poleCenter.y = transform.position.y;
			float sidewaysOffset = -Vector3.Dot((poleCenter - transform.position), transform.parent.transform.right);

			Vector3 offset = endPointTransform.right * sidewaysOffset + endPointTransform.up * poleHeightDifference;
			offset = Vector3.Scale(offset, endPointTransform.localScale);
			activeEndPoint += offset;

			
		}


		vectorFromStartToEnd = activeEndPoint - transform.position;


		// Setting the Start object to look at the end will be used for making the wind be perpendicular to the cable later.
		transform.forward = vectorFromStartToEnd.normalized;
		// Get number of points in the cable using the distance from the start to end, and the point density
		//pointsInLineRenderer = Mathf.FloorToInt(pointDensity * vectorFromStartToEnd.magnitude);
		pointsInLineRenderer = numberOfPoints;//Mathf.FloorToInt(pointDensity * vectorFromStartToEnd.magnitude);
		// Set number of points in line renderer
		line.positionCount = pointsInLineRenderer;

		// The Direction of SAG is the direction of gravity
		sagDirection = Physics.gravity.normalized;

		myStart = transform.position;
		myEnd = endPointTransform.position;
		myEndRot = endPointTransform.localRotation;
		myEndScale = endPointTransform.localScale;

		Draw();
	}


	void Start () 
	{
		line = GetComponent<LineRenderer>();

		if (!endPointTransform)
		{
			Debug.LogWarning("No Endpoint Transform assigned to Cable_Procedural component attached to " + gameObject.name);
			return;
		}

		UpdateEnds();
        Draw();
    }
	


	void Update () 
	{
		if (myEnd != endPointTransform.position || myStart != transform.position || myEndRot != endPointTransform.localRotation || myEndScale != endPointTransform.localScale)
		{
			UpdateEnds();
		}
		//      else if (swayEnabled)
		//      {
		//	Draw();
		//}

	}



	void Draw()
	{
		if (!endPointTransform)
		{
			return;
		}

		// What point is being calculated
		int i = 0;

		swayValue += swayFrequency * Time.deltaTime;

		// Clamp the wind value to stay within a cirlce's radian limits.
		if(swayValue > Mathf.PI * 2){swayValue = 0;}
		if(swayValue < 0){swayValue = Mathf.PI * 2;}


		while(i < pointsInLineRenderer)
		{
			// This is the fraction of where we are in the cable and it accounts for arrays starting at zero.
			float pointForCalcs = (float)i / (pointsInLineRenderer - 1);
			// This is what gives the cable a curve and makes the wind move the center the most.
			float effectAtPointMultiplier = Mathf.Sin(pointForCalcs * Mathf.PI);

			// Calculate the position of the current point i
			Vector3 pointPosition = vectorFromStartToEnd * pointForCalcs;
			// Calculate the sag vector for the current point i
			Vector3 sagAtPoint = sagDirection * sagAmplitude;
			// Calculate the sway vector for the current point i
			Vector3 swayAtPoint = swayMultiplier * transform.TransformDirection( new Vector3(Mathf.Sin(swayValue) * swayXMultiplier, Mathf.Cos(2 * swayValue + Mathf.PI) * .5f * swayYMultiplier, 0));
			// Calculate the waving due to wind for the current point i 

			// Calculate the postion with Sag.
			Vector3 currentPointsPosition = 
				transform.position + 
				pointPosition + 
				(swayAtPoint + 
					Vector3.ClampMagnitude(sagAtPoint, sagAmplitude)) * effectAtPointMultiplier;
		

			// Set point
			line.SetPosition(i, currentPointsPosition);
			i++;
		}
	}
}
