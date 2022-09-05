using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeatDisplayUI : MonoBehaviour
{
    public static HeatDisplayUI Instance { get; private set; }

    public Image myBackground;

    private Color startColour;
    // Start is called before the first frame update
    void Start()
    {
        startColour = myBackground.color;
        myBackground.fillAmount = 0.0f;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetHeatLevel(float heatLevel, bool isOverheated)
    {
        myBackground.fillAmount = Mathf.Clamp01(heatLevel);
        if (isOverheated)
        {
            myBackground.color = new Color(startColour.r, startColour.g, startColour.b, 1);

        }
        else
        {
            myBackground.color = startColour;
        }
    }
}
