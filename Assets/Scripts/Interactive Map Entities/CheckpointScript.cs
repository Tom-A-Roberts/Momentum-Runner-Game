using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("im feeling very triggered");
        Transform parent;
        parent = other.gameObject.transform.parent;

        if (parent != null)
        {
            LevelLogicManager playerLevelScript = other.gameObject.transform.parent.GetComponent<LevelLogicManager>();
            if (playerLevelScript != null)
            {
                playerLevelScript.bodySpawnPosition = transform.position;
                playerLevelScript.feetSpawnPosition = (transform.position - playerLevelScript.bodyFeetOffset);

            }
            }
    }

}
