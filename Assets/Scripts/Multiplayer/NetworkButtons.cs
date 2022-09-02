using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkButtons : MonoBehaviour
{

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if(!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host (or H on keyboard)")) NetworkManager.Singleton.StartHost();
            //if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
            if (GUILayout.Button("Client (or C on keyboard)")) NetworkManager.Singleton.StartClient();
        }

        GUILayout.EndArea();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.Singleton.StartHost();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void Awake()
    {
        //GetComponent<UnityTransport>().SetDebugSimulatorParameters(
        //    packetDelay: 120,
        //    packetJitter: 5,
        //    dropRate: 3
        //    );
    }
}
