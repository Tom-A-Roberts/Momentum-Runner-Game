using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



public class Target: MonoBehaviour , IShootInterface
{
    public GameObject[] ObjectsToChange;

    void IShootInterface.OnHitByLaser()
    {
       foreach (GameObject gb in ObjectsToChange)
        {
            gb.SetActive(!gb.activeSelf);
        }
    }
}
