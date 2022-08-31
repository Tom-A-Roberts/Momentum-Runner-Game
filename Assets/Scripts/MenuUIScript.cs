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

    public void DisableMainPanel()
    {
        MainMenuPanel.SetActive(false);
    }
    public void EnableMainPanel()
    {
        MainMenuPanel.SetActive(false);
    }

    public void DisableSettingsPanel()
    {   
        SettingsPanel.SetActive(false);
    }
    public void EnableSettingsPanel()
    {
        SettingsPanel.SetActive(false);
    }

    public void DisableGamePanel()
    {
        GameModePanel.SetActive(false);
    }
    public void EnableGamePanel()
    {
        GameModePanel.SetActive(false);
    }


    public void QuitButton()
    {
        Debug.Log("game quit");
        Application.Quit();
    }

}
