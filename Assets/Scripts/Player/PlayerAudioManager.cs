using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioManager : NetworkBehaviour
{
    private AudioSource mainAudioSource;
    public float startVolume = 1;
    public float musicVolume = 1;
    public float rollingSoundSmoothAmount = 0.05f;
    public float wallRollingSoundSmoothAmount = 0.05f;
    public float windSoundSmoothAmount = 0.1f;
    public float grappleSoundSmoothAmount = 0.1f;
    public float wallRollingPitchChange = 1f;
    public float rollingPitchChange = 1f;
    public float windPitchChange = 0.6f;
    public float grapplePitchChange = 0.2f;
    public float JumpPitchChange = 0.2f;
    public float JumpPitch = 1.2f;
    public float wallRollingPitch = -1f;
    public float rollingPitch = 0f;
    public float grapplePitch = 1f;
    public float JumpVolume = 0.2f;
    public float ShootVolume = 0.3f;
    public float grappleSwingVolume = 0.3f;
    public AudioClip ambiance;
    public AudioClip rollingLoop;
    public AudioClip wallRollingLoop;
    public AudioClip grapplingSwingingLoop;
    public AudioClip grapplingStart;
    public AudioClip grapplingEnd;
    public AudioClip deathwallLoop;
    public AudioClip jump;
    public AudioClip land;
    public AudioClip wind;
    public AudioClip shoot;
    public AudioClip airdash;
    public AudioClip airJump;
    public AudioClip overheatedGun;
    public AudioClip gunCoolingEffect;
    public AudioClip hitmarkerSound;
    public AudioClip hitSound;

    public AudioClip countdownStart;
    public AudioClip countdownBeep;
    public AudioClip countdownEnd;
    public AudioClip victorySound;
    public AudioClip defeatSound;
    // Start is called before the first frame update

    [SerializeField]
    public bool spectatorMode = false;

    private AudioClip waitingToReadyUpSong;
    private AudioClip levelSoundTracks;
    private float previousCountdownProgress;

    private AudioSource musicSource;
    private AudioSource ambianceSource;
    private AudioSource rollingSource;
    private AudioSource wallRollingSource;
    private AudioSource windSource;
    private AudioSource grapplingSwingingSource;
    private AudioSource shootingSource;
    private AudioSource jumpingSource;
    private AudioSource deathwallSource;

    private float rollingTargetIntensity;
    private float rollingCurrentIntensity;
    private float rollingIntensityDif;

    private float wallRollingTargetIntensity;
    private float wallRollingCurrentIntensity;
    private float wallRollingIntensityDif;

    private float windTargetIntensity;
    private float windCurrentIntensity;
    private float windIntensityDif;

    private float grappleTargetIntensity;
    private float grappleCurrentIntensity;
    private float grappleIntensityDif;

    private SettingsInterface settings;

    void Start()
    {
        mainAudioSource = GetComponent<AudioSource>();

        if (settings == null)
            settings = new SettingsInterface();
        startVolume = settings.effectsVolume.Value;
        musicVolume = settings.musicVolume.Value;

        if (IsOwner)
        {
            ambianceSource = gameObject.AddComponent<AudioSource>();
            ambianceSource.clip = ambiance;
            ambianceSource.volume = startVolume * 0.6f;
            ambianceSource.loop = true;
            ambianceSource.Play();
        }

        rollingSource = gameObject.AddComponent<AudioSource>();
        rollingSource.clip = rollingLoop;
        //rollingSource.volume = 0f;
        rollingSource.pitch = 1f;
        rollingSource.loop = true;
        rollingSource.Play();

        wallRollingSource = gameObject.AddComponent<AudioSource>();
        wallRollingSource.clip = wallRollingLoop;
        wallRollingSource.loop = true;
        wallRollingSource.volume = 0f;
        wallRollingSource.Play();

        windSource = gameObject.AddComponent<AudioSource>();
        windSource.clip = wind;
        windSource.loop = true;
        windSource.volume = 0f;
        windSource.Play();

        deathwallSource = gameObject.AddComponent<AudioSource>();
        deathwallSource.clip = deathwallLoop;
        deathwallSource.loop = true;
        deathwallSource.volume = 0f;
        deathwallSource.Play();

        grapplingSwingingSource = gameObject.AddComponent<AudioSource>();
        grapplingSwingingSource.clip = grapplingSwingingLoop;
        grapplingSwingingSource.loop = true;
        grapplingSwingingSource.volume = 0f;
        grapplingSwingingSource.Play();

        shootingSource = gameObject.AddComponent<AudioSource>();
        shootingSource.clip = shoot;

        jumpingSource = gameObject.AddComponent<AudioSource>();
        jumpingSource.clip = jump;
    }

    public void SetupLevelSoundtracks(AudioClip _waitingToReadyUpSong, AudioClip _levelSoundTracks)
    {
        if(settings == null)
            settings = new SettingsInterface();

        startVolume = settings.effectsVolume.Value;
        musicVolume = settings.musicVolume.Value;

        waitingToReadyUpSong = _waitingToReadyUpSong;
        levelSoundTracks = _levelSoundTracks;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = waitingToReadyUpSong;
        musicSource.volume = musicVolume * 0.9f;

        musicSource.loop = true;
    }
    public void SwitchToReadyUpMusic()
    {
        if (musicSource)
        {
            musicSource.clip = waitingToReadyUpSong;
            musicSource.volume = musicVolume * 0.9f;
            musicSource.loop = true;
            musicSource.PlayDelayed(1);
        }
    }

    public void VictorySound()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(victorySound, startVolume * 0.15f);
    }
    public void DefeatSound()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(defeatSound, startVolume * 0.6f);
    }

    public void HitmarkerSound()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(hitmarkerSound, startVolume * 0.3f);
    }

    public void HitSound()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(hitSound, startVolume * 0.15f);
    }

    public void SwitchToCountdown()
    {
        if (musicSource)
        {
            musicSource.Stop();
        }
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(countdownStart, startVolume * 0.2f);
    }
    public void SwitchToGameplaySoundtrack()
    {
        if (musicSource)
        {
            musicSource.clip = levelSoundTracks;
            musicSource.volume = musicVolume * 0.6f;
            musicSource.loop = true;
            musicSource.PlayDelayed(0);
        }
        //if (mainAudioSource != null)
        //    mainAudioSource.PlayOneShot(countdownEnd, startVolume * 0.3f);

        previousCountdownProgress = 0;
    }

    public void PlaySoundsDuringCountdown(float countdownProgress)
    {
        if(countdownProgress < 5.5f && countdownProgress > 1.5f)
        {
            if (Mathf.FloorToInt(countdownProgress) != Mathf.FloorToInt(previousCountdownProgress))
            {
                if (mainAudioSource != null)
                    mainAudioSource.PlayOneShot(countdownBeep, startVolume * 0.1f);
            }
        }
        else if(countdownProgress < 1.5f && countdownProgress > 0.5f)
        {
            if (Mathf.FloorToInt(countdownProgress) != Mathf.FloorToInt(previousCountdownProgress))
            {
                if (mainAudioSource != null)
                    mainAudioSource.PlayOneShot(countdownEnd, startVolume * 0.1f);
            }
        }

        previousCountdownProgress = countdownProgress;
    }

    public void UpdateWindIntensity(float newIntensity)
    {
        windTargetIntensity = newIntensity;
    }

    public void UpdateRunningIntensity(float newIntensity)
    {
        rollingTargetIntensity = newIntensity;
        //rollingSource.volume = Mathf.Clamp01(newIntensity) * startVolume * 0.4f;
        //const float pitchChange = 0.2f;
        //rollingSource.pitch = 1 + (Mathf.Clamp01(newIntensity) * pitchChange) - (pitchChange / 1);
    }

    public void UpdateWallRunningIntensity(float newIntensity)
    {
        //wallRollingSource.volume = Mathf.Clamp01(newIntensity) * startVolume * 0.4f;
        //const float pitchChange = 0.2f;
        //wallRollingSource.pitch = 1 + (Mathf.Clamp01(newIntensity) * pitchChange) - (pitchChange / 2);

        wallRollingTargetIntensity = newIntensity;
    }

    public void UpdateDeathwallIntensity(float newIntensity)
    {
        if (spectatorMode)
            newIntensity = 0;
        deathwallSource.volume = Mathf.Clamp01(newIntensity) * startVolume * 0.65f;
        const float pitchChange = 0.8f;
        float pitchIntensity = Mathf.Pow(newIntensity, 2);
        deathwallSource.pitch = 1f + (Mathf.Clamp01(pitchIntensity) * pitchChange) - (pitchChange / 2);
    }

    public void UpdateGrappleSwingingIntensity(float newIntensity)
    {
        grappleTargetIntensity = newIntensity;
    }


    public override void OnNetworkSpawn()
    {

    }

    public void Jump()
    {
        jumpingSource.pitch = JumpPitch + (Random.value * JumpPitchChange) - (JumpPitchChange / 2);
        jumpingSource.volume = (Random.value * (JumpVolume * 0.1f) + JumpVolume) * startVolume;

        jumpingSource.Play();

    }
    public void AirJump()
    {
        Jump();
        //if (mainAudioSource != null)
        //    mainAudioSource.PlayOneShot(airJump, (Random.value * (JumpVolume * 0.1f) + JumpVolume) * startVolume);
    }
    public void Land(float power)
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(land, Mathf.Clamp01(power) * startVolume);
    }

    public void Shoot()
    {
        const float pitchChange = 0.13f;
        const float volumeChange = 0.05f;
        shootingSource.pitch = 1 + (Random.value * pitchChange) - (pitchChange / 2);
        shootingSource.volume = (ShootVolume * (1+ (Random.value * volumeChange) - (volumeChange / 2))) * startVolume;
        shootingSource.Play();
    }
    public void AirDash()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(shoot, 0.3f * startVolume);
    }
    public void GrappleStart()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(grapplingStart, 0.2f * startVolume);
    }
    public void GrappleEnd()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(grapplingEnd, 0.3f * startVolume);
    }

    public void OverheatedGun()
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(overheatedGun, 0.3f * startVolume);
    }
    public void GunCoolingEffect(float intensity)
    {
        if (mainAudioSource != null)
            mainAudioSource.PlayOneShot(gunCoolingEffect, 0.4f * startVolume * intensity);
    }

    public void Update()
    { 
        rollingCurrentIntensity = Mathf.SmoothDamp(rollingCurrentIntensity, rollingTargetIntensity, ref rollingIntensityDif, rollingSoundSmoothAmount);
        wallRollingCurrentIntensity = Mathf.SmoothDamp(wallRollingCurrentIntensity, wallRollingTargetIntensity, ref wallRollingIntensityDif, wallRollingSoundSmoothAmount);
        windCurrentIntensity = Mathf.SmoothDamp(windCurrentIntensity, windTargetIntensity, ref windIntensityDif, windSoundSmoothAmount);
        grappleCurrentIntensity = Mathf.SmoothDamp(grappleCurrentIntensity, grappleTargetIntensity, ref grappleIntensityDif, grappleSoundSmoothAmount);

        if (!spectatorMode)
        {
            wallRollingSource.volume = Mathf.Clamp01(wallRollingCurrentIntensity) * startVolume * 0.13f;
            wallRollingSource.pitch = wallRollingPitch + (Mathf.Clamp01(wallRollingCurrentIntensity) * wallRollingPitchChange) - (wallRollingPitchChange / 2);

            rollingSource.volume = Mathf.Clamp01(rollingCurrentIntensity + (wallRollingCurrentIntensity / 8f)) * startVolume * 0.5f;
            rollingSource.pitch = rollingPitch + (Mathf.Clamp01(rollingCurrentIntensity) * rollingPitchChange) - (rollingPitchChange / 2);

            grapplingSwingingSource.volume = Mathf.Clamp01(grappleCurrentIntensity) * startVolume * grappleSwingVolume;
            grapplingSwingingSource.pitch = grapplePitch + (Mathf.Clamp01(grappleCurrentIntensity) * grapplePitchChange) - (grapplePitchChange / 2);

            windSource.volume = Mathf.Clamp01(windCurrentIntensity) * startVolume * 0.2f;
            windSource.pitch = 1 + (Mathf.Clamp01(windCurrentIntensity) * windPitchChange) - (windPitchChange / 2);
        }
        else
        {
            wallRollingSource.volume = 0;
            rollingSource.volume = 0;
            grapplingSwingingSource.volume = 0;
            windSource.volume = 0;
        }
    }
}
