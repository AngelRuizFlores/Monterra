using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioOptionsUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer mixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MasterKey = "MasterVolumeValue";
    private const string MusicKey = "MusicVolumeValue";
    private const string SfxKey = "SfxVolumeValue";

    private bool isInitializing;

    private void Start()
    {
        isInitializing = true;

        LoadSavedValues();

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        isInitializing = false;
    }

    private void LoadSavedValues()
    {
        float master = PlayerPrefs.GetFloat(MasterKey, 1f);
        float music = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfx = PlayerPrefs.GetFloat(SfxKey, 1f);

        masterSlider.value = master;
        musicSlider.value = music;
        sfxSlider.value = sfx;

        ApplyVolume("MasterVolume", master);
        ApplyVolume("MusicVolume", music);
        ApplyVolume("SfxVolume", sfx);
    }

    private void SetMasterVolume(float value)
    {
        ApplyVolume("MasterVolume", value);

        if (!isInitializing)
        {
            PlayerPrefs.SetFloat(MasterKey, value);
            PlayerPrefs.Save();
        }
    }

    private void SetMusicVolume(float value)
    {
        ApplyVolume("MusicVolume", value);

        if (!isInitializing)
        {
            PlayerPrefs.SetFloat(MusicKey, value);
            PlayerPrefs.Save();
        }
    }

    private void SetSfxVolume(float value)
    {
        ApplyVolume("SfxVolume", value);

        if (!isInitializing)
        {
            PlayerPrefs.SetFloat(SfxKey, value);
            PlayerPrefs.Save();
        }
    }

    private void ApplyVolume(string parameterName, float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        mixer.SetFloat(parameterName, Mathf.Log10(value) * 20f);
    }
}