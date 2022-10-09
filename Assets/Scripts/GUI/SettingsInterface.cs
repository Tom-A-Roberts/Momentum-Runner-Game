using Lexic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static SettingsInterface;

public class SettingsInterface
{

    /// <summary>
    /// Static variable that is always updated/synced to playerprefs. Also caches the value to ensure
    /// no unnecessary "gets" to playerprefs.
    /// </summary>
    /// <typeparam name="T">Type, must be either: float, int, string</typeparam> 
    public class SettingsContainer<T>
    {
        /// <summary>
        /// The string value used to save the int into PlayerPrefs
        /// </summary>
        private string prefsName;
        public string PrefsName => prefsName;

        /// <summary>
        /// What to use when first created
        /// </summary>
        private T defaultValue;
        public T DefaultValue => defaultValue;

        private T cachedValue;

        public SettingsContainer(string _name, T _defaultValue)
        {
            if (typeof(T) != typeof(string) && typeof(T) != typeof(int) && typeof(T) != typeof(float))
            {
                Debug.LogError("Player prefs SettingsContainer was instantiated with an unknown type!");
                return;
            }

            prefsName = _name;
            defaultValue = _defaultValue;

            if (!PlayerPrefs.HasKey(prefsName))
            {
                Value = defaultValue;
            }
            else
            {
                cachedValue = GetValueFromMemory();
            }
        }

        public void SetValueFromGeneric(object value)
        {
            Value = (T)Convert.ChangeType(value, typeof(T));
        }

        public T Value
        {
            get
            {
                return cachedValue;
            }
            set
            {
                if(cachedValue.Equals(value))
                {
                    return;
                }

                SetMemoryFromValue(value);
            }
        }

        private T GetValueFromMemory()
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(PlayerPrefs.GetString(prefsName), typeof(string));
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)Convert.ChangeType(PlayerPrefs.GetInt(prefsName), typeof(int));
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)Convert.ChangeType(PlayerPrefs.GetFloat(prefsName), typeof(float));
            }
            else
            {
                Debug.LogError("Player prefs SettingsContainer was instantiated with an unknown type!");
                return default(T);
            }
        }

        private void SetMemoryFromValue(T value)
        {
            cachedValue = value;

            if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(prefsName, (string)Convert.ChangeType(value, typeof(string)));
            }
            else if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(prefsName, (int)Convert.ChangeType(value, typeof(int)));
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(prefsName, (float)Convert.ChangeType(value, typeof(float)));
            }
            else
            {
                Debug.LogError("Player prefs SettingsContainer was instantiated with an unknown type!");
            }
            PlayerPrefs.Save();
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(prefsName);
            SetMemoryFromValue(defaultValue);
        }

        public override string ToString() => Value.ToString();
    }

    public SettingsContainer<int> fpsLimit;

    public SettingsContainer<float> musicVolume;

    public SettingsContainer<float> effectsVolume;

    public SettingsContainer<int> graphicsQuality;

    public SettingsContainer<float> brightness;

    public string DisplayName
    {
        get
        {
            if (PlayerPrefs.HasKey("displayName"))
            {
                return PlayerPrefs.GetString("displayName");
            }
            else
            {
                string newName = NameGen.GetNextRandomName();
                DisplayName = newName;
                return newName;
            }
        }
        set
        {
            PlayerPrefs.SetString("displayName", value);
            PlayerPrefs.Save();
        }
    }


    public SettingsInterface()
    {
        fpsLimit = new SettingsContainer<int>("fpsLimit", _defaultValue: 0);
        musicVolume = new SettingsContainer<float>("musicVolume", _defaultValue: 1);
        effectsVolume = new SettingsContainer<float>("effectsVolume", _defaultValue: 1);
        graphicsQuality = new SettingsContainer<int>("qualityPreset", _defaultValue: 0);
        brightness = new SettingsContainer<float>("brightness", _defaultValue: 0.5f);
    }

    public void RegenerateName()
    {
        string newName = NameGen.GetNextRandomName();
        DisplayName = newName;
    }

    public void ClearSettingsPlayerPrefs()
    {
        //PlayerPrefs.DeleteKey("displayName");

        fpsLimit.Reset();
        musicVolume.Reset();
        effectsVolume.Reset();
        graphicsQuality.Reset();
        brightness.Reset();

    }

}
