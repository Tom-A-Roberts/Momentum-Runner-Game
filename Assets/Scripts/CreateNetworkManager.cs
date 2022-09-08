using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CreateNetworkManager : MonoBehaviour
{
    public GameObject networkPrefabGameobject;

    public bool startAsHost = false;

    void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            Instantiate(networkPrefabGameobject, Vector3.zero, Quaternion.identity);
            Debug.Log("Created new networkmanager");

            if (startAsHost)
            {
                NetworkManager.Singleton.StartHost();
            }
        }
            
    }

    void Start()
    {

    }
}
