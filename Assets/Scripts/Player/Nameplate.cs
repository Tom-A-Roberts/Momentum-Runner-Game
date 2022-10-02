using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Nameplate : MonoBehaviour
{
    [Header("Known Objects")]
    public GameObject background;
    public GameObject backgroundInner;
    public GameObject healthbar;

    [System.NonSerialized]
    public Transform localPlayer;

    public bool Initialized => initialized;
    private bool initialized = false;
    private TMP_Text textObj;

    private float originalWidth;

    // Start is called before the first frame update
    void Start()
    {
        

        if (!initialized)
        {
            GetComponent<MeshRenderer>().enabled = false;
            backgroundInner.GetComponent<MeshRenderer>().enabled = false;
            background.GetComponent<MeshRenderer>().enabled = false;
            healthbar.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (initialized)
        {
            transform.LookAt(localPlayer.position, Vector3.up);
            transform.Rotate(new Vector3(0,180,0), Space.Self);
        }
    }

    public void Init(Transform _localPlayer, string text)
    {
        textObj = GetComponent<TMP_Text>();
        GetComponent<MeshRenderer>().enabled = true;
        backgroundInner.GetComponent<MeshRenderer>().enabled = true;
        background.GetComponent<MeshRenderer>().enabled = true;
        healthbar.GetComponent<MeshRenderer>().enabled = true;

        localPlayer = _localPlayer;
        textObj.text = text;
        initialized = true;
        originalWidth = healthbar.transform.localScale.x;
        SetHealthBar(0);
    }

    public void SetName(string newName)
    {
        if(!textObj){
            textObj = GetComponent<TMP_Text>();
        }

        textObj.text = newName;
    }

    public void SetHealthBar(float health)
    {
        health = Mathf.Clamp01(health);

        healthbar.transform.localScale = new Vector3(originalWidth * health, healthbar.transform.localScale.y, healthbar.transform.localScale.z);
        healthbar.transform.localPosition = new Vector3(-5 * health, healthbar.transform.localPosition.y, healthbar.transform.localPosition.z);
    }

}
