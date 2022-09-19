using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Singleton { get; private set; }

    public GameObject LeaderboardUIObject;

    public bool IsShowing => _isShowing;
    private bool _isShowing = false;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(Singleton);
        }
        Singleton = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!_isShowing)
            HideLeaderboard();
        else
            ShowLeaderboard();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowLeaderboard()
    {
        _isShowing = true;
        LeaderboardUIObject.SetActive(true);
        IngameEscMenu.UnlockCursor();
    }
    public void HideLeaderboard()
    {
        _isShowing = false;
        if (!IngameEscMenu.Singleton.isEscMenuShowing)
            IngameEscMenu.LockCursor();
        LeaderboardUIObject.SetActive(false);
    }
}
