using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 매니저입니다.
/// 싱글톤 패턴으로 구현되어 게임 전체에서 하나의 인스턴스만 존재합니다.
/// 신을 전환할 때 PlayBGM, StopBGM 메서드를 사용하여 배경 음악을 제어할 수 있습니다.
/// 
/// 
/// 구조:
/// AudioManager (Singleton, DontDestroy)
///├── BGM AudioSource(1개)
///├── SFX AudioSource Pool (여러 개)
///└── AudioMixer (볼륨 제어)
///
/// 
/// AudioManager.cs
///
///-Awake()
///- InitPool()
///
///- PlayBGM()
///- FadeBGM()
///
///- PlaySFX()
///- GetAvailableSFX()
///- DisableAfterPlay()
///
///- SetBGMVolume()
///- SetSFXVolume()
///
/// 
/// 다른 스크립에서 사용 예시:
/// AudioManager.Instance.PlaySFX(goPlaceSound); <- for SFX
/// AudioManager.Instance.PlayBGM(menuMusic); <- for BGM
/// AudioManager.Instance.SetBGMVolume(value); <- for BGM volume control UI Slider
/// </summary>

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    public AudioMixer mixer;

    [Header("BGM")]
    public AudioSource bgmSource;

    [Header("SFX")]
    public AudioSource sfxPrefab;
    public int poolSize = 10;

    private List<AudioSource> sfxPool = new List<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitPool();
    }

    void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource sfx = Instantiate(sfxPrefab, transform);
            sfx.gameObject.SetActive(false);
            sfxPool.Add(sfx);
        }
    }
    public void PlayBGM(AudioClip clip, float fadeTime = 0.5f)
    {
        if (bgmSource.clip == clip) return;

        StopAllCoroutines();
        StartCoroutine(FadeBGM(clip, fadeTime));
    }

    IEnumerator FadeBGM(AudioClip newClip, float duration)
    {
        float startVolume = bgmSource.volume;

        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        while (bgmSource.volume < startVolume)
        {
            bgmSource.volume += startVolume * Time.deltaTime / duration;
            yield return null;
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        AudioSource sfx = GetAvailableSFX();

        sfx.clip = clip;
        sfx.volume = volume;
        sfx.gameObject.SetActive(true);
        sfx.Play();

        StartCoroutine(DisableAfterPlay(sfx));
    }

    AudioSource GetAvailableSFX()
    {
        foreach (var sfx in sfxPool)
        {
            if (!sfx.gameObject.activeInHierarchy)
                return sfx;
        }

        AudioSource newSfx = Instantiate(sfxPrefab, transform);
        sfxPool.Add(newSfx);
        return newSfx;
    }

    IEnumerator DisableAfterPlay(AudioSource sfx)
    {
        yield return new WaitForSeconds(sfx.clip.length);
        sfx.gameObject.SetActive(false);
    }

    public void SetBGMVolume(float value)
    {
        mixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20);
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }
}
