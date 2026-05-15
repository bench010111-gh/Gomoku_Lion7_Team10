using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;

    [Header("Sliders")]
    public Slider bgmSlider;
    //public Slider sfxSlider;

    [Header("Optional Text")]
    public TMP_Text bgmValueText;
    public TMP_Text sfxValueText;

    private const string PREF_BGM_VOLUME = "BGM_VOLUME";
    private const string PREF_SFX_VOLUME = "SFX_VOLUME";

    private void Start()
    {
        float savedBgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.7f);
        float savedSfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0.0001f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = savedBgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        //if (sfxSlider != null)
        //{
        //    sfxSlider.minValue = 0.0001f;
        //    sfxSlider.maxValue = 1f;
        //    sfxSlider.value = savedSfxVolume;
        //    sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        //}

        ApplyBgmVolume(savedBgmVolume);
        ApplySfxVolume(savedSfxVolume);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);

        //if (sfxSlider != null)
        //    sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    public void OpenSettings()
    {
        PlayClickSound();

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayClickSound();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnBgmVolumeChanged(float value)
    {
        ApplyBgmVolume(value);

        PlayerPrefs.SetFloat(PREF_BGM_VOLUME, value);
        PlayerPrefs.Save();
    }

    public void OnSfxVolumeChanged(float value)
    {
        ApplySfxVolume(value);

        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, value);
        PlayerPrefs.Save();
    }

    private void ApplyBgmVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }

        if (bgmValueText != null)
        {
            bgmValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    private void ApplySfxVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        if (sfxValueText != null)
        {
            sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }
    }
}