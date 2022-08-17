using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Speedometer : MonoBehaviour
{
    [Tooltip("The text element that should display the current speed")]
    public TextMeshProUGUI speedometerText;
    [Tooltip("The rigidbody that the speedometer should track the speed of")]
    public Rigidbody bodyToTrack;
    // Start is called before the first frame update
    void Start()
    {
        if(bodyToTrack == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning("NO PLAYER FOUND! Speedometer won't work.");
            }
            bodyToTrack = player.GetComponent<Rigidbody>();
            if (bodyToTrack == null)
            {
                Debug.LogWarning("NO RIGID BODY ON PLAYER FOUND! Speedometer won't work.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (bodyToTrack != null)
        {
            int bodySpeed = Mathf.RoundToInt(bodyToTrack.velocity.magnitude);
            if (bodySpeed > 999)
            {
                bodySpeed = 999;
            }
            speedometerText.text = bodySpeed.ToString("D3");
        }
    }
}
