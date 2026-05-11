using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioManager
/// - Singleton
/// - Persistent between scenes
/// - BGM crossfade
/// - SFX pooling
/// - Mixer volume control
/// </summary>

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    public AudioMixer mixer;

    [Header("BGM Sources (Crossfade)")]
    public AudioSource bgmSourceA;
    public AudioSource bgmSourceB;

    private AudioSource currentBGM;
    private AudioSource nextBGM;

    private Coroutine bgmRoutine;

    [Header("SFX Pool")]
    public AudioSource sfxPrefab;
    public int poolSize = 10;

    private List<AudioSource> sfxPool = new List<AudioSource>();

    [Header("Default SFX Clips")]
    public AudioClip stonePlaceClip;
    public AudioClip buttonClickClip;
    public AudioClip shutterCrashClip;
    public AudioClip popupClip;
    public AudioClip winClip;
    public AudioClip lossClip;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentBGM = bgmSourceA;
        nextBGM = bgmSourceB;

        InitPool();
    }

    // =========================================================
    // SFX POOL INIT
    // =========================================================

    void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource sfx = Instantiate(sfxPrefab, transform);

            // Keep active permanently
            sfx.gameObject.SetActive(true);

            sfxPool.Add(sfx);
        }
    }

    // =========================================================
    // BGM
    // =========================================================

    public void PlayBGM(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null)
            return;

        if (currentBGM.clip == clip)
            return;

        CrossfadeBGM(clip, fadeTime, true);
    }

    public void CrossfadeBGM(AudioClip newClip, float duration = 1.5f, bool loop = true)
    {
        if (newClip == null)
            return;

        // stop only current bgm coroutine
        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        bgmRoutine = StartCoroutine(CrossfadeCoroutine(newClip, duration, loop));
    }

    IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration, bool loop)
    {
        nextBGM.clip = newClip;
        nextBGM.loop = loop;
        nextBGM.volume = 0f;

        nextBGM.Play();

        float time = 0f;

        float startCurrentVolume = currentBGM.volume;
        float targetVolume = 0.3f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            currentBGM.volume = Mathf.Lerp(startCurrentVolume, 0f, t);
            nextBGM.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        currentBGM.volume = 0f;
        currentBGM.Stop();

        nextBGM.volume = targetVolume;

        // swap
        AudioSource temp = currentBGM;
        currentBGM = nextBGM;
        nextBGM = temp;

        bgmRoutine = null;
    }

    // =========================================================
    // INTRO -> LOOP
    // =========================================================

    public void PlayIntroThenLoop(AudioClip intro, AudioClip loop, float crossfadeTime = 1.5f)
    {
        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        bgmRoutine = StartCoroutine(IntroThenLoopCoroutine(intro, loop, crossfadeTime));
    }

    IEnumerator IntroThenLoopCoroutine(AudioClip intro, AudioClip loop, float crossfadeTime)
    {
        currentBGM.clip = intro;
        currentBGM.loop = false;
        currentBGM.volume = 1f;

        currentBGM.Play();

        yield return new WaitForSeconds(intro.length - crossfadeTime);

        CrossfadeBGM(loop, crossfadeTime, true);
    }

    // =========================================================
    // SFX
    // =========================================================

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        AudioSource sfx = GetAvailableSFX();

        sfx.clip = clip;
        sfx.volume = volume;

        sfx.Play();
    }

    AudioSource GetAvailableSFX()
    {
        // find available source
        foreach (var sfx in sfxPool)
        {
            if (!sfx.isPlaying)
            {
                return sfx;
            }
        }

        // expand pool if needed
        AudioSource newSfx = Instantiate(sfxPrefab, transform);

        newSfx.gameObject.SetActive(true);

        sfxPool.Add(newSfx);

        return newSfx;
    }

    // =========================================================
    // VOLUME
    // =========================================================

    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        mixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20);
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }

    // =========================================================
    // DEFAULT SFX HELPERS
    // =========================================================

    public void PlayStoneSound()
    {
        if (stonePlaceClip != null)
        {
            PlaySFX(stonePlaceClip);
        }
    }

    public void PlayClickSound()
    {
        if (buttonClickClip != null)
        {
            PlaySFX(buttonClickClip);
        }
    }

    public void PlayShutterSound()
    {
        if (shutterCrashClip != null)
        {
            PlaySFX(shutterCrashClip);
        }
    }

    public void PlayPopupSound()
    {
        if (popupClip != null)
        {
            PlaySFX(popupClip);
        }
    }

    public void PlayWinSound()
    {
        if (winClip != null)
        {
            PlaySFX(winClip);
        }
    }

    public void PlayLossSound()
    {
        if (lossClip != null)
        {
            PlaySFX(lossClip);
        }
    }
}