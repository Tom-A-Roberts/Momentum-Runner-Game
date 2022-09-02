using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIScript : MonoBehaviour
{
    public AudioClip clickSoundEffect;
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject GameModePanel;
    public GameObject LevelSelectPanel;
    public bool isMultiplayer;

    private AudioSource myAudioSource;


    public void Start()
    {
        myAudioSource = GameObject.FindObjectOfType<AudioSource>();

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
    }
    public void EnableMainPanel()
    {
        MainMenuPanel.SetActive(true);
    }
    public void EnableSettingsPanel()
    {
        SettingsPanel.SetActive(true);
    }
    public void EnableLevelSelect()
    {
        LevelSelectPanel.SetActive(true);
    }

    public void DisableGamePanel()
    {
        GameModePanel.SetActive(false);
        ButtonClicked();
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
    }

    public void LoadExampleLevel()
    {
        SceneManager.LoadScene("LevelExample",LoadSceneMode.Single);
    }
    public void SingleplayerLevelSelect()
    {
        isMultiplayer = false;
    }
    public void MultiplayerLevelSelect()
    {
        isMultiplayer = true;
    }
    public void QuitButton()
    {
        Debug.Log("game quit");
        Application.Quit();
    }

}
