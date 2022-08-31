using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIScript : MonoBehaviour
{

    SceneManager sceneManager = new SceneManager();
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject GameModePanel;

    public void SingleplayerLevelSelect()
    {
        //temporarily disable main menu
        //enable level select screen
    }

    public void LoadGameModePanel()
    {   
        GameModePanel.SetActive(true);
        MainMenuPanel.SetActive(false);
    }
    public void GameModeBacktoMenuButton()
    {
        MainMenuPanel.SetActive(true);
        GameModePanel.SetActive(false);
    }

    public void SettingsBacktoMenuButton()
    {
        MainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
    }

    public void SettingsScreenLoad()
    {
        SettingsPanel.SetActive(true);
        MainMenuPanel.SetActive(false);
    }
    public void QuitButton()
    {
        Debug.Log("game quit");
        Application.Quit();
    }

}
