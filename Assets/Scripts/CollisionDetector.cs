using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public LevelController levelController;
    public float coyoteTime = 0.5f;
    private bool m_IsOnGround;
    private bool m_IsOnGroundCoyote;
    private float coyoteTimer = 0;
    private bool cyoteEnded = false;


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

    public bool IsOnGroundCoyote
    {
        get
        {

            if (m_IsOnGroundCoyote)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void Update()
    {
        m_IsOnGroundCoyote = m_IsOnGround;

        if(coyoteTimer > 0)
        {
            coyoteTimer -= Time.deltaTime / coyoteTime;
            if(coyoteTimer < 0)
            {
                coyoteTimer = 0;
            }
            m_IsOnGroundCoyote = true;
        }

        if (m_IsOnGround && !cyoteEnded)
        {
            m_IsOnGroundCoyote = true;
            coyoteTimer = 1;
        }

        if (cyoteEnded && !m_IsOnGround)
        {
            cyoteEnded = false;
        }
    }

    public void EndCoyoteTime()
    {
        coyoteTimer = 0;
        m_IsOnGroundCoyote = false;
        cyoteEnded = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "TriggerDeath")
        {
            levelController.PlayerDeath();
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
