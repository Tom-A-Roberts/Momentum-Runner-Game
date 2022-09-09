using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using UnityEngine.UI;
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

    // Sliders
    public Slider musicVolumeSlider;
    public Slider effectsVolumeSlider;

    public bool isMultiplayerHosting;

    private AudioSource myAudioSource;
    private AudioSource effectsAudioSource;
    private string hostingIp;
    private string hostingPort;
    private string clientingIp;
    private string clientingPort;

    public static bool joinAsClient = false;
    public static bool startNetworkingOnSpawn = true;

    public static float musicVolume = 1;
    public static float effectsVolume = 1;


    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;

        myAudioSource = GameObject.FindObjectOfType<AudioSource>();
        effectsAudioSource = myAudioSource.gameObject.AddComponent<AudioSource>();
        effectsAudioSource.volume = 1;
        UpdatePortFieldsFromPrefs();
        UpdateAudioSlidersFromPrefs();
        joinAsClient = false;
    }

    public void ButtonClicked()
    {
        
        if (effectsAudioSource != null)
        {
            effectsAudioSource.PlayOneShot(clickSoundEffect);
        }
    }

    public void QuitButton()
    {
        Debug.Log("game quit");
        Application.Quit();
    }

    #region Settings Menu

    public void EffectsVolumeChanged()
    {
        effectsVolume = effectsVolumeSlider.value;
        UpdateVolumes();
        PlayerPrefs.SetFloat("effectsVolume", effectsVolume);
        PlayerPrefs.SetInt("volumeSettingsRemembered", 1);
        PlayerPrefs.Save();
    }
    public void MusicVolumeChanged()
    {
        musicVolume = musicVolumeSlider.value;
        UpdateVolumes();
        PlayerPrefs.SetFloat("musicVolume", musicVolume);
        PlayerPrefs.SetInt("volumeSettingsRemembered", 1);
        PlayerPrefs.Save();
    }

    public static void UpdateAudioStaticsFromPrefs()
    {
        if(PlayerPrefs.GetInt("volumeSettingsRemembered") == 1)
        {
            musicVolume = PlayerPrefs.GetFloat("musicVolume");
            effectsVolume = PlayerPrefs.GetFloat("effectsVolume");
        }
        else
        {
            musicVolume = 1;
            effectsVolume = 1;
        }

    }

    private void UpdateAudioSlidersFromPrefs()
    {
        if(PlayerPrefs.GetInt("volumeSettingsRemembered") == 1)
        {
            UpdateAudioStaticsFromPrefs();
            effectsVolumeSlider.value = effectsVolume;
            musicVolumeSlider.value = musicVolume;
        }
        else
        {
            effectsVolume = effectsVolumeSlider.value;
            musicVolume = musicVolumeSlider.value;
            PlayerPrefs.SetFloat("musicVolume", musicVolume);
            PlayerPrefs.SetFloat("effectsVolume", effectsVolume);
            PlayerPrefs.SetInt("volumeSettingsRemembered", 1);
            PlayerPrefs.Save();
        }

    }
    public void UpdateVolumes()
    {
        effectsAudioSource.volume = effectsVolume;
        myAudioSource.volume = musicVolume * 0.7f;
    }
    

    #endregion


    #region Panel management

    public void EnableGamePanel()
    {
        GameModePanel.SetActive(true);

        UpdatePortFieldsFromPrefs();
    }
    public void EnableMainPanel()
    {
        MainMenuPanel.SetActive(true);
        
    }
    public void EnableSettingsPanel()
    {
        SettingsPanel.SetActive(true);
        UpdateAudioSlidersFromPrefs();
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

    #endregion



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

    private void UpdatePortFieldsFromPrefs()
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
