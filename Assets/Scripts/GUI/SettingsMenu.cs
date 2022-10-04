using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{

    //#region Settings Menu

    //public void EffectsVolumeChanged()
    //{
    //    effectsVolume = effectsVolumeSlider.value;
    //    UpdateVolumes();
    //    PlayerPrefs.SetFloat("effectsVolume", effectsVolume);
    //    PlayerPrefs.SetInt("volumeSettingsRemembered", 1);
    //    PlayerPrefs.Save();
    //}
    //public void MusicVolumeChanged()
    //{
    //    musicVolume = musicVolumeSlider.value;
    //    UpdateVolumes();
    //    PlayerPrefs.SetFloat("musicVolume", musicVolume);
    //    PlayerPrefs.SetInt("volumeSettingsRemembered", 1);
    //    PlayerPrefs.Save();
    //}

    //public void DisplayNameChanged()
    //{
    //    UpdateDisplayName(displayName.text);
    //    displayName.text = localDisplayName;
    //}
    //public void RegenerateName()
    //{
    //    localDisplayName = "";
    //    PlayerPrefs.SetString("displayName", localDisplayName);
    //    PlayerPrefs.Save();
    //    UpdateDisplayName();
    //    displayName.text = localDisplayName;
    //}

    //public void UpdateFPSLimit()
    //{
    //    int newLim = 0;
    //    if (fpsLimitInput.text.Length != 0)
    //    {
    //        newLim = int.Parse(fpsLimitInput.text);
    //    }

    //    bool passed = true;
    //    if (newLim < 0)
    //    {
    //        passed = false;
    //    }
    //    if (newLim > 0 && newLim < 15)
    //    {
    //        passed = false;
    //    }
    //    if (newLim > 999)
    //    {
    //        passed = false;
    //    }
    //    if (passed)
    //    {
    //        fpsLimit = newLim;
    //    }

    //    string limTex = fpsLimit.ToString();
    //    if (limTex == "0")
    //        limTex = "";
    //    fpsLimitInput.text = limTex;

    //}

    //public static void UpdateAudioStaticsFromPrefs()
    //{
    //    if (PlayerPrefs.GetInt("volumeSettingsRemembered") == 1)
    //    {
    //        musicVolume = PlayerPrefs.GetFloat("musicVolume");
    //        effectsVolume = PlayerPrefs.GetFloat("effectsVolume");
    //    }
    //    else
    //    {
    //        musicVolume = 1;
    //        effectsVolume = 1;
    //    }

    //}

    //private void UpdateSettingsFromPrefs()
    //{
    //    if (PlayerPrefs.GetInt("volumeSettingsRemembered") == 1)
    //    {
    //        UpdateAudioStaticsFromPrefs();
    //        effectsVolumeSlider.value = effectsVolume;
    //        musicVolumeSlider.value = musicVolume;
    //    }
    //    else
    //    {
    //        effectsVolume = effectsVolumeSlider.value;
    //        musicVolume = musicVolumeSlider.value;
    //        PlayerPrefs.SetFloat("musicVolume", musicVolume);
    //        PlayerPrefs.SetFloat("effectsVolume", effectsVolume);
    //        PlayerPrefs.SetInt("volumeSettingsRemembered", 1);
    //        PlayerPrefs.Save();
    //    }

    //    string limTex = fpsLimit.ToString();
    //    if (limTex == "0")
    //        limTex = "";
    //    fpsLimitInput.text = limTex;

    //    UpdateDisplayName();
    //    displayName.text = localDisplayName;
    //}

    //public void UpdateVolumes()
    //{
    //    effectsAudioSource.volume = effectsVolume;
    //    myAudioSource.volume = musicVolume * 0.7f;
    //}


    //#endregion

}
