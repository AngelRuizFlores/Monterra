using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class AudioInfo
{
    public AudioClip clip;
    public string AudioName;
    public AudioMixerGroup Mixer;

    [Range(0f, 1f)]
    public float Volume = 1f;
}

public sealed class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance => _instance;

    public List<AudioInfo> Audios = new List<AudioInfo>();

    private readonly List<AudioSource> audioManager = new List<AudioSource>();
    private Dictionary<string, AudioInfo> clipList;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        BuildClipLookup();
        CacheAudioSources();
    }

    public bool TryPlaySound(string name, bool loop)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning($"{nameof(SoundManager)} recibió un nombre de audio vacío.", this);
            return false;
        }

        if (clipList == null)
        {
            BuildClipLookup();
        }

        if (!clipList.TryGetValue(name, out AudioInfo audioInfo) || audioInfo == null || audioInfo.clip == null)
        {
            Debug.LogWarning($"No existe un audio válido con el nombre '{name}'.", this);
            return false;
        }

        AudioSource source = GetAvailableAudioSource();
        if (source == null)
        {
            Debug.LogError("No hay AudioSource disponible para reproducir el sonido.", this);
            return false;
        }

        source.clip = audioInfo.clip;
        source.loop = loop;
        source.volume = Mathf.Clamp01(audioInfo.Volume);
        source.outputAudioMixerGroup = audioInfo.Mixer;
        source.Play();
        return true;
    }

    public void PlaySound(string name, bool loop)
    {
        TryPlaySound(name, loop);
    }

    public void ResetSound()
    {
        for (int i = 0; i < audioManager.Count; i++)
        {
            AudioSource source = audioManager[i];
            if (source == null)
                continue;

            source.Stop();
            source.clip = null;
            source.loop = false;
        }
    }

    public void Play(string name, bool loop)
    {
        TryPlaySound(name, loop);
    }

    private void BuildClipLookup()
    {
        clipList = new Dictionary<string, AudioInfo>(StringComparer.Ordinal);

        for (int i = 0; i < Audios.Count; i++)
        {
            AudioInfo audioInfo = Audios[i];
            if (audioInfo == null)
                continue;

            if (string.IsNullOrWhiteSpace(audioInfo.AudioName))
            {
                Debug.LogWarning("Se ha omitido un audio con nombre vacío.", this);
                continue;
            }

            if (audioInfo.clip == null)
            {
                Debug.LogWarning($"El audio '{audioInfo.AudioName}' no tiene clip asignado.", this);
                continue;
            }

            if (clipList.ContainsKey(audioInfo.AudioName))
            {
                Debug.LogWarning($"Audio duplicado detectado: '{audioInfo.AudioName}'.", this);
                continue;
            }

            clipList.Add(audioInfo.AudioName, audioInfo);
        }
    }

    private void CacheAudioSources()
    {
        audioManager.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            AudioSource source = transform.GetChild(i).GetComponent<AudioSource>();
            if (source != null)
            {
                audioManager.Add(source);
            }
        }

        if (audioManager.Count == 0)
        {
            audioManager.Add(gameObject.AddComponent<AudioSource>());
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        for (int i = 0; i < audioManager.Count; i++)
        {
            AudioSource source = audioManager[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        AudioSource extraSource = gameObject.AddComponent<AudioSource>();
        audioManager.Add(extraSource);
        return extraSource;
    }
}