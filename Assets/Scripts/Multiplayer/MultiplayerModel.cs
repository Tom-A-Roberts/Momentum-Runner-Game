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
        int layer = GetLayer(isOwner);

        gameObject.layer = layer;

        foreach (GameObject part in parts)
        {
            part.layer = layer;
        }
    }

    private int GetLayer(bool isOwner)
    {
        // OR: having the layers hardcoded is ok for now
        return (isOwner) ? LayerMask.NameToLayer("FirstPersonRendering") : LayerMask.NameToLayer("Default");
    }
}
