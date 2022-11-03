using UnityEngine;

// Thanks to: https://answers.unity.com/questions/118306/grouping-objects-in-the-hierarchy.html
public class UnrootChildrenAndDeleteOnRunTime : MonoBehaviour
{
    void Awake()
    {
        while (transform.childCount > 0)
        {
            transform.GetChild(0).parent = null;
        }
        Destroy(this.gameObject);
    }
}