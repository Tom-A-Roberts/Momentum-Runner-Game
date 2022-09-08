using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;

public class MenuUIScript : NetworkBehaviour
{
    public AudioClip clickSoundEffect;
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject GameModePanel;
    public GameObject LevelSelectPanel;

    public GameObject ConnectingToServerText;

    // Text inputs:
    public TMP_InputField hostingIpText;
    public TMP_InputField hostingPortText;
    public TMP_InputField clientingIpText;
    public TMP_InputField clientingPortText;

    public bool isMultiplayerHosting;

    private AudioSource myAudioSource;
    private string hostingIp;
    private string hostingPort;
    private string clientingIp;
    private string clientingPort;

    public static bool joinAsClient = false;
    public static bool startNetworkingOnSpawn = true;


    public void Start()
    {
        myAudioSource = GameObject.FindObjectOfType<AudioSource>();
        UpdateFieldsFromPrefs();
        joinAsClient = false;
    }

    public void ButtonClicked()
    {
        
        if (myAudioSource != null)
        {
            myAudioSource.PlayOneShot(clickSoundEffect);
        }
    }

    public void EnableGamePanel()
    {
        GameModePanel.SetActive(true);

        UpdateFieldsFromPrefs();
    }
    public void EnableMainPanel()
    {
        MainMenuPanel.SetActive(true);
        
    }
    public void EnableSettingsPanel()
    {
        SettingsPanel.SetActive(true);
    }


    public void DisableGamePanel()
    {
        GameModePanel.SetActive(false);
        ButtonClicked();
        ConnectingToServerText.SetActive(false);

    }
    public void DisableMainPanel()
    {
        MainMenuPanel.SetActive(false);
        ButtonClicked();
    }
    public void DisableSettingsPanel()
    {   
        SettingsPanel.SetActive(false);
        ButtonClicked();
    }




    //public void MultiplayerLevelSelect()
    //{
    //    isMultiplayerHosting = true;
    //}
    public void DisableLevelSelect()
    {
        LevelSelectPanel.SetActive(false);
        ButtonClicked();
        isMultiplayerHosting = false;
    }
    public void EnableLevelSelect()
    {
        LevelSelectPanel.SetActive(true);
    }

    public void QuitButton()
    {
        Debug.Log("game quit");
        Application.Quit();
    }

    #region Server Hosting and Joining


    public void LoadExampleLevelAsHost()
    {
        joinAsClient = false;
        startNetworkingOnSpawn = true;

        SceneManager.LoadScene("LevelExample", LoadSceneMode.Single);
        //NetworkManager.SceneManager.LoadScene("LevelExample", LoadSceneMode.Single);
    }
    public void LoadExampleLevel2AsHost()
    {
        joinAsClient = false;
        startNetworkingOnSpawn = true;

        SceneManager.LoadScene("LevelExample 2", LoadSceneMode.Single);
        //NetworkManager.SceneManager.LoadScene("LevelExample", LoadSceneMode.Single);
    }

    
    public void SingleplayerLevelSelect()
    {
        DisableGamePanel();
        EnableLevelSelect();
        isMultiplayerHosting = false;

        UnityTransport transportScript = UnityTransport.FindObjectOfType<UnityTransport>();
        transportScript.ConnectionData.Address = "127.0.0.1";
        transportScript.ConnectionData.Port = (ushort)7777;


    }

    public void HostNewServer()
    {
        DisableGamePanel();
        EnableLevelSelect();
        isMultiplayerHosting = true;
        // Check IP address and port right here
        UnityTransport transportScript = UnityTransport.FindObjectOfType<UnityTransport>();
        transportScript.ConnectionData.Address = hostingIp;
        transportScript.ConnectionData.Port = ushort.Parse(hostingPort);
        transportScript.ConnectionData.ServerListenAddress = "0.0.0.0";

        PlayerPrefs.SetString("hostingIp", hostingIp);
        PlayerPrefs.SetString("hostingPort", hostingPort);
        PlayerPrefs.Save();

    }
    public void DirectConnectToServer()
    {
        // Check IP address and port right here
        UnityTransport transportScript = UnityTransport.FindObjectOfType<UnityTransport>();
        transportScript.ConnectionData.Address = clientingIp;
        transportScript.ConnectionData.Port = ushort.Parse(clientingPort);
        transportScript.ConnectionData.ServerListenAddress = "0.0.0.0";

        PlayerPrefs.SetString("clientingIp", clientingIp);
        PlayerPrefs.SetString("clientingPort", clientingPort);
        PlayerPrefs.Save();

        joinAsClient = true;
        startNetworkingOnSpawn = false;

        NetworkManager.Singleton.StartClient();

        ConnectingToServerText.SetActive(true);
        
    }


    #endregion



    #region textfields

    private void UpdateFieldsFromPrefs()
    {
        hostingIp = PlayerPrefs.GetString("hostingIp");
        hostingPort = PlayerPrefs.GetString("hostingPort");
        clientingIp = PlayerPrefs.GetString("clientingIp");
        clientingPort = PlayerPrefs.GetString("clientingPort");

        hostingIpText.text = hostingIp;
        hostingPortText.text = hostingPort;
        clientingIpText.text = clientingIp;
        clientingPortText.text = clientingPort;
    }

    public void UpdateHostingIp()
    {
        hostingIp = hostingIpText.text;
    }
    public void UpdateHostingPort()
    {
        hostingPort = hostingPortText.text;
    }
    public void UpdateClientingIp()
    {
        clientingIp = clientingIpText.text;
    }
    public void UpdateClientingPort()
    {
        clientingPort = clientingPortText.text;
    }

    #endregion
}
