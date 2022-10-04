using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using TMPro;
using UnityEngine.SceneManagement;

public class IngameEscMenu : MonoBehaviour
{
    public static IngameEscMenu Singleton { get; private set; }

    public GameObject recoveringText;
    public GameObject escapeMenuUIObject;
    public GameObject hitmarkerUIElement;
    public bool isEscMenuShowing = false;
    public bool curserUnlocked = false;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(Singleton);
        }
        Singleton = this;
    }

    void Start()
    {
        if (isEscMenuShowing)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isEscMenuShowing)
            Hide();
        else if (Input.GetKeyDown(KeyCode.Escape))
            Show();
    }

    public void ShowRecoveringInfo()
    {
        if (recoveringText)
        {
            recoveringText.SetActive(true);
        }
    }
    public void HideRecoveringInfo()
    {
        if (recoveringText)
        {
            recoveringText.SetActive(false);
        }
    }

    public void Show()
    {
        escapeMenuUIObject.SetActive(true);
        isEscMenuShowing = true;
        UnlockCursor();
    }

    public void Hide()
    {
        escapeMenuUIObject.SetActive(false);
        isEscMenuShowing = false;

        if(!LeaderboardUI.Singleton.IsShowing)
            LockCursor();
    }

    private delegate void OnNetworkShutdown();

    public void LoadMainMenu()
    {
        StartCoroutine(NetworkShutdown(GoToMainMenu));
    }

    private void GoToMainMenu()
    {
        LoadingScreen.Load("Menu");
    }

    public void QuitToDesktop()
    {
        StartCoroutine(NetworkShutdown(QuitApplication));
    }

    public void ReplayLevelButton()
    {
        if (NetworkManager.Singleton.IsHost)
            GameStateManager.Singleton.ResetLevelToBeginning();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        Debug.Log("game quit");
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }   

    // OR: Need to wait for NetworkManager to shutdown fully before we quit
    private IEnumerator NetworkShutdown(OnNetworkShutdown OnShutdown)
    {
        NetworkManager netInstance = NetworkManager.Singleton;

        if (netInstance)
        {
            netInstance.Shutdown();

            while (netInstance.ShutdownInProgress)
                yield return null;
        }

        // :/ just for pressing stop in editor
        if (this)
            OnShutdown();
    }

    public static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Singleton.curserUnlocked = false;
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Singleton.curserUnlocked = true;
    }
}
