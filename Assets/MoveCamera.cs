using UnityEngine;
public class MoveCamera : MonoBehaviour
{
    public Transform CameraPositon;

    private void Update()
    {
        transform.position = CameraPositon.transform.position;
    }
}
