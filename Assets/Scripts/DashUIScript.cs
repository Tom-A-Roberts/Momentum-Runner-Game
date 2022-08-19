using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashUIScript : MonoBehaviour
{
    [SerializeField]
    private Image imageCooldown;

    [SerializeField]
    private TMP_Text textCooldown;

    public PlayerController pc;
    private bool isOnCooldown = false;
    private float coolDownTimer;


    // Start is called before the first frame update
    void Start()
    {
        imageCooldown.fillAmount = 0.0f;
        textCooldown.text = "Q"; //Again later change this to not be hardcoded
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) UseDash(); //change this to a rebindable dash key later.
        if (isOnCooldown)
        {
            ApplyCooldown();
        }
    }

    void ApplyCooldown()
    {
        coolDownTimer -=Time.deltaTime;
        if (coolDownTimer < 0.0f)
        {
            isOnCooldown = false;
            textCooldown.text = "Q"; //Again later change this to not be hardcoded
            imageCooldown.fillAmount = 0.0f;
        }
        else
        {
            textCooldown.text = Mathf.RoundToInt(coolDownTimer).ToString();
            imageCooldown.fillAmount = coolDownTimer / pc.DashCooldown;
        }

    }

    public bool UseDash()
    {
        if (isOnCooldown)
        {
            return false;
        }
        else
        {
            isOnCooldown = true;
            textCooldown.gameObject.SetActive(true);
            coolDownTimer = pc.DashCooldown;
            return true;
        
        }
    }
}
