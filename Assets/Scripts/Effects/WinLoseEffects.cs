using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class WinLoseEffects : MonoBehaviour
{
    public static WinLoseEffects Singleton { get; private set; }

    public GameObject victoryImage;
    public GameObject defeatImage;

    [Header("Prefabs")]
    public GameObject victoryParticles;
    public GameObject defeatParticles;

    /// <summary>
    /// Depth effect to enable when someone has won or someone has lost
    /// </summary>
    [System.NonSerialized]
    public VolumeComponent depthEffect;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
            Destroy(Singleton);
        Singleton = this;
    }

    public void StartWinEffects()
    {
        victoryImage.SetActive(true);
        if (!depthEffect.active)
            depthEffect.active = true;

        if (GameStateManager.Singleton && GameStateManager.Singleton.localPlayer)
        {
            Quaternion upDirection = Quaternion.FromToRotation(Vector3.forward, GameStateManager.Singleton.localPlayer.myCamera.transform.up); ;
            Instantiate(victoryParticles, GameStateManager.Singleton.localPlayer.bodyRigidbody.position, upDirection);
        }
    }

    public void StartLoseEffects()
    {
        defeatImage.SetActive(true);
        if (!depthEffect.active)
            depthEffect.active = true;

        if (GameStateManager.Singleton && GameStateManager.Singleton.localPlayer)
        {
            Quaternion upDirection = Quaternion.FromToRotation(Vector3.forward, GameStateManager.Singleton.localPlayer.myCamera.transform.up); ;
            Instantiate(defeatParticles, GameStateManager.Singleton.localPlayer.bodyRigidbody.position, upDirection);
        }
    }
    public void EndEffects()
    {
        victoryImage.SetActive(false);
        defeatImage.SetActive(false);
        if (depthEffect.active)
            depthEffect.active = false;

    }

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Rendering.Volume[] sceneVolumes = GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>();
        foreach (var sceneVolume in sceneVolumes)
        {
            if (sceneVolume != null)
            {
                for (int componentID = 0; componentID < sceneVolume.profile.components.Count; componentID++)
                {
                    if (sceneVolume.profile.components[componentID].name.Contains("DepthOfField"))
                    {
                        depthEffect = (DepthOfField)sceneVolume.profile.components[componentID];
                    }
                }
            }
        }

        EndEffects();
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
