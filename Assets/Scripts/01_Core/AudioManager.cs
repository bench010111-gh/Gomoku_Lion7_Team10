using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 매니저입니다.
/// 싱글톤 패턴으로 구현되어 게임 전체에서 하나의 인스턴스만 존재합니다.
/// DontDestroyOnLoad를 사용하여 씬 전환 시에도 유지됩니다.
///
/// ✔ 주요 기능:
/// - BGM 재생 및 페이드 인/아웃
/// - BGM 크로스페이드 (부드러운 전환)
/// - Intro → Loop 자동 연결
/// - SFX 풀링 시스템 (성능 최적화)
/// - AudioMixer 기반 볼륨 제어
///
/// ------------------------------------------------------------
/// 📌 구조:
/// AudioManager (Singleton, DontDestroy)
/// ├── BGM AudioSource A (크로스페이드용)
/// ├── BGM AudioSource B (크로스페이드용)
/// ├── SFX AudioSource Pool (여러 개)
/// └── AudioMixer (볼륨 제어)
///
/// ------------------------------------------------------------
/// 📌 주요 메서드:
///
/// [초기화]
/// - Awake() → 싱글톤 설정 및 유지
/// - InitPool() → SFX 풀 생성
///
/// [BGM]
/// - PlayBGM() → 일반 BGM 재생 (페이드 포함)
/// - CrossfadeBGM() → 다른 BGM으로 부드럽게 전환
/// - PlayIntroThenLoop() → 인트로 후 루프 자동 연결
///
/// [SFX]
/// - PlaySFX() → 효과음 재생 (풀링)
/// - GetAvailableSFX() → 사용 가능한 AudioSource 반환
/// - DisableAfterPlay() → 재생 후 비활성화
///
/// [Volume]
/// - SetBGMVolume() → BGM 볼륨 조절 (Mixer)
/// - SetSFXVolume() → SFX 볼륨 조절 (Mixer)
///
/// ------------------------------------------------------------
/// 📌 다른 스크립트에서 사용 예시:
///
/// // 🔊 효과음
/// AudioManager.Instance.PlaySFX(gunSound);
///
/// // 🎵 BGM 재생
/// AudioManager.Instance.PlayBGM(menuMusic);
///
/// // 🎵 크로스페이드 전환
/// AudioManager.Instance.CrossfadeBGM(battleMusic);
///
/// // 🎬 Intro → Loop
/// AudioManager.Instance.PlayIntroThenLoop(introBGM, mainBGM);
///
/// // 🎚️ 볼륨 UI
/// AudioManager.Instance.SetBGMVolume(value);
///
/// ------------------------------------------------------------
/// ⚠️ 주의:
/// - AudioManager는 씬에 하나만 존재해야 합니다.
/// - bgmSource는 2개 필요합니다 (크로스페이드용).
/// - sfxPrefab은 AudioSource가 붙은 prefab이어야 합니다.
/// - Mixer의 Exposed Parameter 이름은 정확히 일치해야 합니다.
/// </summary>
/// 

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    public AudioMixer mixer;

    [Header("BGM Sources (2 for crossfade)")]
    public AudioSource bgmSourceA;
    public AudioSource bgmSourceB;

    private AudioSource currentBGM;
    private AudioSource nextBGM;

    [Header("SFX Pool")]
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

        currentBGM = bgmSourceA;
        nextBGM = bgmSourceB;

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

    // =========================
    // 🎵 BGM BASIC
    // =========================

    public void PlayBGM(AudioClip clip, float fadeTime = 1f)
    {
        if (currentBGM.clip == clip) return;

        CrossfadeBGM(clip, fadeTime, true);
    }

    public void CrossfadeBGM(AudioClip newClip, float duration = 1.5f, bool loop = true)
    {
        StopAllCoroutines();
        StartCoroutine(CrossfadeCoroutine(newClip, duration, loop));
    }

    IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration, bool loop)
    {
        nextBGM.clip = newClip;
        nextBGM.loop = loop;
        nextBGM.volume = 0f;
        nextBGM.Play();

        float time = 0f;
        float startVolume = currentBGM.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            currentBGM.volume = Mathf.Lerp(startVolume, 0f, t);
            nextBGM.volume = Mathf.Lerp(0f, 0.3f, t);

            yield return null;
        }

        currentBGM.Stop();

        // swap
        AudioSource temp = currentBGM;
        currentBGM = nextBGM;
        nextBGM = temp;
    }

    // =========================
    // 🎬 INTRO → LOOP
    // =========================

    public void PlayIntroThenLoop(AudioClip intro, AudioClip loop, float crossfadeTime = 1.5f)
    {
        StopAllCoroutines();
        StartCoroutine(IntroThenLoopCoroutine(intro, loop, crossfadeTime));
    }

    IEnumerator IntroThenLoopCoroutine(AudioClip intro, AudioClip loop, float crossfadeTime)
    {
        currentBGM.clip = intro;
        currentBGM.loop = false;
        currentBGM.volume = 1f;
        currentBGM.Play();

        // intro 끝나기 전에 크로스페이드 시작
        yield return new WaitForSeconds(intro.length - crossfadeTime);

        CrossfadeBGM(loop, crossfadeTime, true);
    }

    // =========================
    // 🔊 SFX SYSTEM (POOLING)
    // =========================

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

    // =========================
    // 🎛️ VOLUME CONTROL (MIXER)
    // =========================

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
}