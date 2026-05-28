using UnityEngine;

public class MusicGame : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private string battleMusic;
    [SerializeField] private string worldMusic;

    public void StartFightMusic()
    {
        SoundManager.Instance.ResetSound();
        PlaywithLoop(battleMusic);
    }

    public void StartWorldMusic()
    {
        SoundManager.Instance.ResetSound();
        PlaywithLoop(worldMusic);
    }

    public void PlaywithLoop(string name)
    {
        SoundManager.Instance.PlaySound(name, true);
    }

    public void PlaywithOutLoop(string name)
    {
        SoundManager.Instance.PlaySound(name, false);
    }

    public void StopMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResetSound();
        }
    }
}