using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


public class AdjustSettingsFromPrefs
{
    // Start is called before the first frame update

    private Exposure exposureVolumeProfile;
    private float originalSceneExposure;
    private SettingsInterface settings;


    public AdjustSettingsFromPrefs()
    {
        settings = new SettingsInterface();

        Volume[] sceneVolumes = GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>();
        foreach (var sceneVolume in sceneVolumes)
        {
            if (sceneVolume != null)
            {
                for (int componentID = 0; componentID < sceneVolume.profile.components.Count; componentID++)
                {
                    if (sceneVolume.profile.components[componentID].name.Contains("Exposure"))
                    {
                        exposureVolumeProfile = (Exposure)sceneVolume.profile.components[componentID];
                        originalSceneExposure = exposureVolumeProfile.fixedExposure.value;
                    }
                }
            }
        }

        if (exposureVolumeProfile == null)
        {
            Debug.LogWarning("No exposure found in this scene in any volume profiles! Cannot adjust brightness.");
        }
    }

    public void UpdateGraphics(bool changeExpensiveSettings = true)
    {
        int fpsLimit = settings.fpsLimit.Value;
        if (fpsLimit > 0)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fpsLimit;
        }
        else
        {
            Application.targetFrameRate = -1;
        }

        if (exposureVolumeProfile != null)
        {
            exposureVolumeProfile.fixedExposure.value = originalSceneExposure + (-settings.brightness.Value + 0.5f) * 2f;
        }

        int currentQualityLevel = QualitySettings.GetQualityLevel();

        if(currentQualityLevel != settings.graphicsQuality.Value)
        {
            QualitySettings.SetQualityLevel(settings.graphicsQuality.Value, changeExpensiveSettings);
            Debug.Log("Changed quality setting to: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }



    }
}
