using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsLoader : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    private const string MasterKey = "MasterVolumeValue";
    private const string MusicKey = "MusicVolumeValue";
    private const string SfxKey = "SfxVolumeValue";

    private void Awake()
    {
        ApplySavedAudioSettings();
    }

    private void Start()
    {
        ApplySavedAudioSettings();
    }

    private void ApplySavedAudioSettings()
    {
        if (mixer == null)
            return;

        ApplyVolume("MasterVolume", PlayerPrefs.GetFloat(MasterKey, 1f));
        ApplyVolume("MusicVolume", PlayerPrefs.GetFloat(MusicKey, 1f));
        ApplyVolume("SfxVolume", PlayerPrefs.GetFloat(SfxKey, 1f));
    }

    private void ApplyVolume(string parameterName, float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        mixer.SetFloat(parameterName, Mathf.Log10(value) * 20f);
    }
}