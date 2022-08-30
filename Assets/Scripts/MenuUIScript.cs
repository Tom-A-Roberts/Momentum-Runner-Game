using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIScript : MonoBehaviour
{

    SceneManager sceneManager = new SceneManager();


    public void SingleplayerLevelSelect()
    {
        //temporarily disable main menu
        //enable level select screen
    }

    public void SettingsScreenLoad()
    {
        //temporarily disable main menu panel
        //enable settings panel
    }
    public void QuitButton()
    {
        Application.Quit();
    }

}
