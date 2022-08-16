using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    private bool m_IsOnGround;

    public bool IsOnGround
    {
        get
        {
            if (m_IsOnGround)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    void OnCollisionStay()
    {
        m_IsOnGround = true;
    }
    private void OnCollisionExit(Collision collision)
    {
        m_IsOnGround = false;
    }
}
