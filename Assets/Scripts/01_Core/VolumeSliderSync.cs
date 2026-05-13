using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderSync : MonoBehaviour
{
    public enum AudioType { BGM, SFX }
    public AudioType type;
    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        if (AudioManager.Instance == null) return;

        if (type == AudioType.BGM)
            slider.value = AudioManager.Instance.GetBGMVolume();
        else
            slider.value = AudioManager.Instance.GetSFXVolume();
    }
}
