using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndgameUI : MonoBehaviour
{
    public GameObject endgameMenuUIObject;

    void Start()
    {
        Hide(false);
    }

    public void Hide(bool hideCursor)
    {
        endgameMenuUIObject.SetActive(false);
        if (hideCursor)
            IngameEscMenu.LockCursor();
        
    }
    public void Show()
    {
        IngameEscMenu.UnlockCursor();
        endgameMenuUIObject.SetActive(true);
    }

}
