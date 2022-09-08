using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

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

    public void LoadMainMenu()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        
    }
    public void QuitToDesktop()
    {
        Debug.Log("game quit");
        Application.Quit();
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
