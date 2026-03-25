using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    [Header("Video UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    private Resolution[] availableResolutions;
    private readonly List<Resolution> uniqueResolutions = new();

    private const string ResolutionIndexKey = "ResolutionIndex";
    private const string FullscreenKey = "Fullscreen";
    private const string QualityKey = "QualityLevel";

    private bool isInitializing;

    private void Start()
    {
        isInitializing = true;

        SetupResolutionDropdown();
        SetupQualityDropdown();
        LoadSavedSettings();
        HookEvents();

        isInitializing = false;
    }

    private void SetupResolutionDropdown()
    {
        availableResolutions = Screen.resolutions;
        uniqueResolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        HashSet<string> added = new HashSet<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution resolution = availableResolutions[i];
            string label = resolution.width + "x" + resolution.height;

            if (added.Contains(label))
                continue;

            added.Add(label);
            uniqueResolutions.Add(resolution);
            options.Add(label);

            if (resolution.width == Screen.currentResolution.width &&
                resolution.height == Screen.currentResolution.height)
            {
                currentResolutionIndex = uniqueResolutions.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupQualityDropdown()
    {
        qualityDropdown.ClearOptions();

        List<string> qualityNames = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(qualityNames);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    private void LoadSavedSettings()
    {
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        fullscreenToggle.isOn = isFullscreen;
        Screen.fullScreen = isFullscreen;

        int savedQuality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        savedQuality = Mathf.Clamp(savedQuality, 0, QualitySettings.names.Length - 1);
        qualityDropdown.value = savedQuality;
        qualityDropdown.RefreshShownValue();
        QualitySettings.SetQualityLevel(savedQuality);

        int savedResolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, resolutionDropdown.value);
        savedResolutionIndex = Mathf.Clamp(savedResolutionIndex, 0, uniqueResolutions.Count - 1);
        resolutionDropdown.value = savedResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        ApplyResolution(savedResolutionIndex);
    }

    private void HookEvents()
    {
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void OnResolutionChanged(int index)
    {
        if (isInitializing)
            return;

        ApplyResolution(index);
        PlayerPrefs.SetInt(ResolutionIndexKey, index);
        PlayerPrefs.Save();
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        if (isInitializing)
            return;

        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnQualityChanged(int qualityIndex)
    {
        if (isInitializing)
            return;

        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(QualityKey, qualityIndex);
        PlayerPrefs.Save();
    }

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= uniqueResolutions.Count)
            return;

        Resolution resolution = uniqueResolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}