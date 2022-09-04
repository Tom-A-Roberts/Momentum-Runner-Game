using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



public class Target : MonoBehaviour
{
    public GameObject Door;

    public void TargetHit()
    {
        Door.SetActive(false);
    }

}
