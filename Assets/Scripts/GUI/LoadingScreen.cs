using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadingScreen : MonoBehaviour
{
    public TMP_Text percentageText;

    public bool loadMenuOnStart = false;

    public bool IsShowing => isShowing;
    private bool isShowing = false;

    public float LoadingProgress => loadingProgress;
    private float loadingProgress = 0;

    private AsyncOperation currentAsyncOperation = null;

    public static LoadingScreen Singleton { get; private set; }

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
        Disable();

        if (loadMenuOnStart)
        {
            Load("Menu");
        }
    }

    void Update()
    {
        if(isShowing && currentAsyncOperation != null)
        {

            loadingProgress = Mathf.Clamp01(currentAsyncOperation.progress / 0.9f);
            if (percentageText)
            {
                if(Mathf.RoundToInt(loadingProgress * 100) == 0)
                {
                    percentageText.text = "";
                }
                else
                {
                    percentageText.text = Mathf.RoundToInt(loadingProgress * 100).ToString() + "%";
                }
                
            }
        }
    }

    public void Enable(AsyncOperation _operation)
    {
        
        isShowing = true;
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    public void Disable()
    {
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.SetActive(false);
        }
        isShowing = false;
    }

    public static void Load(string levelName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);

        if (Singleton != null)
        
            Singleton.Enable(asyncOperation);
        
    }
}
