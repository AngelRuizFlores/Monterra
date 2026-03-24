using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    [SerializeField] private string menuMusicName;

    private void Start()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.ResetSound();
        SoundManager.Instance.PlaySound(menuMusicName, true);
    }

    public void StopMenuMusic()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.ResetSound();
    }
}