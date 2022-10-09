using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using UnityEngine.UI;
using Lexic;

//[RequireComponent(typeof(Settings))]
public class MenuUIScript : NetworkBehaviour
{

    [Header("Panels")]
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject GameModePanel;
    public GameObject LevelSelectPanel;

    [Header("Known Settings Objects")]
    public TMP_InputField displayNameInput;
    public TMP_InputField fpsLimitInput;
    public Slider musicVolumeSlider;
    public Slider effectsVolumeSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown graphicsQualityDropdown;

    public GameObject ConnectingToServerText;

    [Header("Known IP Inputs")]
    public TMP_InputField hostingIpText;
    public TMP_InputField hostingPortText;
    public TMP_InputField clientingIpText;
    public TMP_InputField clientingPortText;

    [Header("Other")]
    public bool isMultiplayerHosting;
    public AudioClip clickSoundEffect;

    public SettingsInterface Settings => settings;
    private SettingsInterface settings;

    public AdjustSettingsFromPrefs SettingsAdjuster => settingsAdjuster;
    private AdjustSettingsFromPrefs settingsAdjuster;

    private AudioSource myAudioSource;
    private AudioSource effectsAudioSource;

    private string hostingIp;
    private string hostingPort;
    private string clientingIp;
    private string clientingPort;

    public static bool joinAsClient = false;
    public static bool startNetworkingOnSpawn = true;

    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;

        settings = new SettingsInterface();
        settingsAdjuster = new AdjustSettingsFromPrefs();

        myAudioSource = GameObject.FindObjectOfType<AudioSource>();
        effectsAudioSource = myAudioSource.gameObject.AddComponent<AudioSource>();
        effectsAudioSource.volume = 1;
        UpdateMenuVolumes();
        settingsAdjuster.UpdateGraphics();
        UpdatePortFieldsFromPrefs();
        UpdateSettingsPage();
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

    public void MusicVolumeChanged()
    {
        settings.musicVolume.Value = musicVolumeSlider.value;
        UpdateMenuVolumes();
    }
    public void EffectsVolumeChanged()
    {
        settings.effectsVolume.Value = effectsVolumeSlider.value;
        UpdateMenuVolumes();
    }

    public void BrightnessChanged()
    {
        settings.brightness.Value = brightnessSlider.value;
        settingsAdjuster.UpdateGraphics();
    }

    public void GraphicsQualityChanged()
    {
        settings.graphicsQuality.Value = graphicsQualityDropdown.value;
        settingsAdjuster.UpdateGraphics();
    }

    public void DisplayNameChanged()
    {
        string newText = displayNameInput.text;
        if (newText.Length == 0)
        {
            settings.RegenerateName();
        }
        else
        {
            settings.DisplayName = newText;
            displayNameInput.text = newText;
        }
    }

    public void FPSLimitChanged()
    {
        int newLim = 0;
        if (fpsLimitInput.text.Length != 0)
        {
            newLim = int.Parse(fpsLimitInput.text);
        }

        bool passed = true;
        if (newLim < 0)
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
            settings.fpsLimit.Value = newLim;

            settingsAdjuster.UpdateGraphics();
        }

        string limTex = settings.fpsLimit.Value.ToString();
        
        if (limTex == "0")
            limTex = "";
        fpsLimitInput.text = limTex;
    }

    public void RegenerateName()
    {
        settings.RegenerateName();
        displayNameInput.text = settings.DisplayName;
    }

    public void UpdateSettingsPage()
    {
        effectsVolumeSlider.value = settings.effectsVolume.Value;
        musicVolumeSlider.value = settings.musicVolume.Value;

        string limTex = settings.fpsLimit.Value.ToString();
        if (limTex == "0")
            limTex = "";
        fpsLimitInput.text = limTex;

        displayNameInput.text = settings.DisplayName;

        graphicsQualityDropdown.value = settings.graphicsQuality.Value;

        brightnessSlider.value = settings.brightness.Value;
    }

    public void UpdateMenuVolumes()
    {
        effectsAudioSource.volume = settings.effectsVolume.Value;
        myAudioSource.volume = settings.musicVolume.Value * 0.7f;
    }

    //public void UpdateGraphics()
    //{
    //    if (exposureVolumeProfile == null)
    //    {
    //        Volume[] sceneVolumes = GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>();
    //        foreach (var sceneVolume in sceneVolumes)
    //        {
    //            if (sceneVolume != null)
    //            {
    //                for (int componentID = 0; componentID < sceneVolume.profile.components.Count; componentID++)
    //                {
    //                    if (sceneVolume.profile.components[componentID].name.Contains("Exposure"))
    //                    {
    //                        exposureVolumeProfile = (Exposure)sceneVolume.profile.components[componentID];
    //                        originalSceneExposure = exposureVolumeProfile.fixedExposure.value;
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    if (exposureVolumeProfile != null)
    //    {
    //        exposureVolumeProfile.fixedExposure.value = originalSceneExposure + (-settings.brightness.Value + 0.5f) * 2.5f;
    //    }
    //    else
    //    {
    //        Debug.LogWarning("No exposure found in this scene in any volume profiles! Cannot adjust brightness.");
    //    }

    //}

    public void ResetSettings()
    {
        settings.ClearSettingsPlayerPrefs();
        UpdateSettingsPage();
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
        UpdateSettingsPage();
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

        LoadingScreen.Load("LevelExample");
    }

    public void LoadMultiplayerLevel1AsHost()
    {
        joinAsClient = false;
        startNetworkingOnSpawn = true;

        LoadingScreen.Load("MultiplayerLevel1");
    }

    public void LoadMultiplayerLevel2AsHost()
    {
        joinAsClient = false;
        startNetworkingOnSpawn = true;

        LoadingScreen.Load("MultiplayerLevel 2");
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
