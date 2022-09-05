using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GrapplingRope : MonoBehaviour
{
    public GrappleGun GrappleGun;
    public int quality = 500;
    public float damper = 14f;
    public float strength = 800f;
    public float extendSpeed = 15f;
    public float retractSpeed = 9f;
    public float waveCount = 4f;
    public float waveHeight = 6f;
    public float maxExtendAnimTime = 0.25f;
    public float maxRetractAnimTime = 0.15f;
    public AnimationCurve affectCurve;

    private Vector3 currentRopePosition;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private Spring spring;
    private LineRenderer lr;
    private bool ropeEnabled = false;
    private bool extending = false;
    private float animTime;
    private float animTimeRatio;
    private float targetAnimTime;

    private void Awake()
    {
        if (!TryGetComponent(out lr))        
            Debug.LogError("No LineRenderer found!");       

        spring = new Spring();
        spring.SetTarget(0);
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void SetRopeParams(Vector3 start, Vector3 target, float maxAnimTime, float _speed)
    {
        startPosition = start;
        targetPosition = target;

        animTimeRatio = (startPosition - targetPosition).sqrMagnitude / (GrappleGun.MaxGrappleLength * GrappleGun.MaxGrappleLength);
        targetAnimTime = animTimeRatio * maxAnimTime;

        animTime = 0;

        spring.SetVelocity(_speed);
    }

    public void Extend(Vector3 target)
    {
        SetRopeParams(GrappleGun.GunEndPosition.position, target, maxExtendAnimTime, extendSpeed);

        if (lr.positionCount == 0)
        {
            lr.positionCount = quality + 1;
        }

        extending = true;
        ropeEnabled = true;
    }

    public void Retract()
    {
        SetRopeParams(targetPosition, GrappleGun.GunEndPosition.position, maxRetractAnimTime, retractSpeed);

        extending = false;
    }

    void DrawRope()
    {
        Vector3 gunTipPosition = GrappleGun.GunEndPosition.position;

        if (ropeEnabled)
        {
            if (!extending)
            {
                if (currentRopePosition == gunTipPosition)
                {
                    spring.Reset();

                    ropeEnabled = false;

                    if (lr.positionCount > 0)
                        lr.positionCount = 0;

                    return;
                }

                targetPosition = gunTipPosition;
            }
            else
            {
                startPosition = gunTipPosition;
            }

            spring.SetDamper(damper);
            spring.SetStrength(strength);
            spring.Update(Time.deltaTime);            

            animTime += Time.deltaTime;

            Vector3 up = Quaternion.LookRotation((targetPosition - startPosition).normalized) * Vector3.up;

            currentRopePosition = Vector3.Lerp(startPosition, targetPosition, animTime / targetAnimTime);

            for (int i = 0; i < quality + 1; i++)
            {
                float delta = i / (float)quality;
                Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

                lr.SetPosition(i, Vector3.Lerp(gunTipPosition, currentRopePosition, delta) + offset);
            }
        }
    }
}
