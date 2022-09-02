using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerModel : MonoBehaviour
{
    public List<GameObject> parts;
    public void NetworkInitialize(bool isOwner)
    {
        // OR: currently just stops remote player weapons from showing through objects
        // but could do more in the future e.g. loading different models for third person
        if (!isOwner)
        {
            gameObject.layer = LayerMask.NameToLayer("Default");

            foreach (GameObject part in parts)
            {
                part.layer = LayerMask.NameToLayer("Default");
            }
        }
    }
}
