using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIScript : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject GameModePanel;
    public GameObject LevelSelectPanel;
    public bool isMultiplayer;

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
    }
    public void DisableMainPanel()
    {
        MainMenuPanel.SetActive(false);
    }
    public void DisableSettingsPanel()
    {   
        SettingsPanel.SetActive(false);
    }
    public void DisableLevelSelect()
    {
        LevelSelectPanel.SetActive(false);
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
