using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CornerPlacer : MonoBehaviour
{

    public Transform[] corners;
    public Vector3[] cornerOriginalPositions;
    public GameObject particles;
    private Vector3 lastRecordedSize;

    private bool initialized = false;
    // Start is called before the first frame update
    void Init()
    {
        lastRecordedSize = transform.localScale;
        initialized = true;
        //cornerOriginalPositions = new Vector3[corners.Length];
        //for (int i = 0; i < corners.Length; i++)
        //{
        //    cornerOriginalPositions[i] = corners[i].localPosition;
        //}

        UpdateCorners();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            Init();
        }
        if(transform.localScale != lastRecordedSize)
        {
            UpdateCorners();
            lastRecordedSize = transform.localScale;
        }
    }
    void UpdateCorners()
    {
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i].localPosition = Vector3.Scale(cornerOriginalPositions[i], transform.localScale /2);
        }
        particles.transform.localScale = transform.localScale;
    }
}
