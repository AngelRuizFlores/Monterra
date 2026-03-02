using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicGame : MonoBehaviour
{
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
        SoundManager.Instance.PlaySound( name, true);
    }
    public void PlaywithOutLoop(string name)
    {
        SoundManager.Instance.PlaySound(name, false);
    }
}