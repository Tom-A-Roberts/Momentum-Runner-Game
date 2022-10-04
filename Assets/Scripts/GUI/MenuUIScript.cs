using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using UnityEngine.UI;
using Lexic;

public class MenuUIScript : NetworkBehaviour
{
    public AudioClip clickSoundEffect;
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject GameModePanel;
    public GameObject LevelSelectPanel;

    public GameObject ConnectingToServerText;

    // Text inputs:
    public TMP_InputField displayName;
    public TMP_InputField fpsLimitInput;

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

    public static string localDisplayName = "";

    public static int fpsLimit
    {
        get
        {
            if (!PlayerPrefs.HasKey("fpsLimit"))
            {
                PlayerPrefs.SetInt("fpsLimit", 0);
                PlayerPrefs.Save();
                return 0;
            }

            return PlayerPrefs.GetInt("fpsLimit");
        }
        set
        {
            PlayerPrefs.SetInt("fpsLimit", value);
            PlayerPrefs.Save();
        }
    }

    public static float musicVolume = 1;
    public static float effectsVolume = 1;


    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;

        myAudioSource = GameObject.FindObjectOfType<AudioSource>();
        effectsAudioSource = myAudioSource.gameObject.AddComponent<AudioSource>();
        effectsAudioSource.volume = 1;
        UpdateDisplayName();
        UpdatePortFieldsFromPrefs();
        UpdateSettingsFromPrefs();
        joinAsClient = false;
    }

    public static void UpdateDisplayName(string newName = "")
    {
        if(newName.Length == 0)
        {
            localDisplayName = PlayerPrefs.GetString("displayName");
            if (localDisplayName.Length == 0)
            {
                localDisplayName = NameGen.GetNextRandomName();
                PlayerPrefs.SetString("displayName", localDisplayName);
                PlayerPrefs.Save();
            }
        }
        else
        {
            localDisplayName = newName;
            PlayerPrefs.SetString("displayName", localDisplayName);
            PlayerPrefs.Save();
        }


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

    public void DisplayNameChanged()
    {
        UpdateDisplayName(displayName.text);
        displayName.text = localDisplayName;
    }
    public void RegenerateName()
    {
        localDisplayName = "";
        PlayerPrefs.SetString("displayName", localDisplayName);
        PlayerPrefs.Save();
        UpdateDisplayName();
        displayName.text = localDisplayName;
    }

    public void UpdateFPSLimit()
    {
        int newLim = 0;
        if (fpsLimitInput.text.Length != 0)
        {
            newLim = int.Parse(fpsLimitInput.text);
        }
        
        bool passed = true;
        if(newLim < 0)
        {
            passed = false;
        }
        if (newLim > 0 && newLim < 15)
        {
            passed = false;
        }
        if (newLim > 999)
        {
            passed = false;
        }
        if (passed)
        {
            fpsLimit = newLim;
        }

        string limTex = fpsLimit.ToString();
        if (limTex == "0")
            limTex = "";
        fpsLimitInput.text = limTex;

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

    private void UpdateSettingsFromPrefs()
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

        string limTex = fpsLimit.ToString();
        if (limTex == "0")
            limTex = "";
        fpsLimitInput.text = limTex;

        UpdateDisplayName();
        displayName.text = localDisplayName;
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
        UpdateSettingsFromPrefs();
    }

    public void DisableGamePanel()
    {
        GameModePanel.SetActive(false);
        ButtonClicked();
        CancelConnectingState();
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

    public void LoadMultiplayerLevel1AsHost()
    {
        joinAsClient = false;
        startNetworkingOnSpawn = true;

        SceneManager.LoadScene("MultiplayerLevel1", LoadSceneMode.Single);
        //NetworkManager.SceneManager.LoadScene("LevelExample", LoadSceneMode.Single);
    }

    public void LoadMultiplayerLevel2AsHost()
    {
        joinAsClient = false;
        startNetworkingOnSpawn = true;

        SceneManager.LoadScene("MultiplayerLevel 2", LoadSceneMode.Single);
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

        isMultiplayerHosting = false;

        PlayerPrefs.SetString("clientingIp", clientingIp);
        PlayerPrefs.SetString("clientingPort", clientingPort);
        PlayerPrefs.Save();

        joinAsClient = true;
        startNetworkingOnSpawn = false;

        NetworkManager.Singleton.StartClient();

        ConnectingToServerText.SetActive(true);
    }

    public void CancelConnectingState()
    {

        joinAsClient = false;
        startNetworkingOnSpawn = false;

        isMultiplayerHosting = false;

        NetworkManager.Singleton.Shutdown();

        ConnectingToServerText.SetActive(false);

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
