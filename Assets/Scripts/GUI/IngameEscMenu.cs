using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;

public class IngameEscMenu : MonoBehaviour
{
    public static IngameEscMenu Instance { get; private set; }

    public GameObject escapeMenuUIObject;
    public bool isEscMenuShowing = false;
    public bool curserUnlocked = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }

        Instance = this;
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
        LockCursor();
    }

    private delegate void OnNetworkShutdown();

    public void LoadMainMenu()
    {
        StartCoroutine(NetworkShutdown(GoToMainMenu));
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    public void QuitToDesktop()
    {
        StartCoroutine(NetworkShutdown(QuitApplication));
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
        Instance.curserUnlocked = false;
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Instance.curserUnlocked = true;
    }
}
