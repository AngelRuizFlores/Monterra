using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EndGameSequenceController : MonoBehaviour
{
    private const float DefaultDisplaySeconds = 5f;

    [Header("Dependencies")]
    [SerializeField] private MainMenuLoader mainMenuLoader;
    [SerializeField] private PlayerTeam playerTeam;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private MusicGame music;
    [SerializeField] private GameObject generatorsRoot;

    [Header("Canvas")]
    [SerializeField] private GameObject endGameCanvasRoot;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image playerImage;
    [SerializeField] private Image[] monImages = new Image[6];

    [Header("Audio Names")]
    [SerializeField] private string victoryMusicName = "VictoryMusic";
    [SerializeField] private string defeatMusicName = "DefeatMusic";

    [Header("Timing")]
    [SerializeField] private float displaySeconds = DefaultDisplaySeconds;

    private Coroutine runningSequence;

    public void PlayVictorySequence()
    {
        StartSequence(true);
    }

    public void PlayDefeatSequence()
    {
        StartSequence(false);
    }

    private void Awake()
    {
        if (endGameCanvasRoot != null)
            endGameCanvasRoot.SetActive(false);
    }

    private void StartSequence(bool victory)
    {
        if (runningSequence != null)
            StopCoroutine(runningSequence);

        runningSequence = StartCoroutine(PlaySequenceCoroutine(victory));
    }

    private IEnumerator PlaySequenceCoroutine(bool victory)
    {
        Time.timeScale = 1f;

        if (generatorsRoot != null)
            generatorsRoot.SetActive(false);

        PlayEndMusic(victory);
        BuildEndScreen(victory);

        if (endGameCanvasRoot != null)
            endGameCanvasRoot.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, displaySeconds));

        if (mainMenuLoader == null)
        {
            Debug.LogError($"{nameof(EndGameSequenceController)}: falta asignar {nameof(MainMenuLoader)}.", this);
            yield break;
        }

        mainMenuLoader.LoadMainMenu();
    }

    private void PlayEndMusic(bool victory)
    {
        string musicName = victory ? victoryMusicName : defeatMusicName;

        if (music != null)
        {
            music.StopMusic();
            music.PlaywithOutLoop(musicName);
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResetSound();
            SoundManager.Instance.Play(musicName, false);
            return;
        }

        Debug.LogWarning($"{nameof(EndGameSequenceController)}: no hay {nameof(MusicGame)} ni {nameof(SoundManager)} disponibles para reproducir la música final.", this);
    }

    private void BuildEndScreen(bool victory)
    {
        if (resultText != null)
            resultText.text = victory ? "VICTORY" : "DEFEAT";

        if (playerImage != null)
        {
            playerImage.enabled = false;

            if (playerSpriteRenderer != null && playerSpriteRenderer.sprite != null)
            {
                playerImage.sprite = playerSpriteRenderer.sprite;
                playerImage.enabled = true;
                playerImage.preserveAspect = true;
            }
        }

        ClearMonImages();

        if (playerTeam == null)
        {
            Debug.LogWarning($"{nameof(EndGameSequenceController)}: falta asignar {nameof(PlayerTeam)}.", this);
            return;
        }

        List<MonInstance> ownedMons = playerTeam.GetOwnedMons();
        if (ownedMons == null || ownedMons.Count == 0)
            return;

        int count = Mathf.Min(monImages.Length, ownedMons.Count);

        for (int i = 0; i < count; i++)
        {
            Image slot = monImages[i];
            MonInstance mon = ownedMons[i];

            if (slot == null || mon == null || mon.species == null)
                continue;

            Sprite sprite = TryResolveMonSprite(mon.species);
            if (sprite == null)
                continue;

            slot.sprite = sprite;
            slot.enabled = true;
            slot.color = Color.white;
            slot.preserveAspect = true;
        }
    }

    private void ClearMonImages()
    {
        if (monImages == null)
            return;

        for (int i = 0; i < monImages.Length; i++)
        {
            if (monImages[i] == null)
                continue;

            monImages[i].sprite = null;
            monImages[i].enabled = false;
        }
    }

    private static Sprite TryResolveMonSprite(object species)
    {
        if (species == null)
            return null;

        string[] candidateNames =
        {
            "menuSprite",
            "iconSprite",
            "frontSprite",
            "battleSprite",
            "sprite"
        };

        Type type = species.GetType();

        for (int i = 0; i < candidateNames.Length; i++)
        {
            FieldInfo field = type.GetField(candidateNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
            {
                Sprite value = field.GetValue(species) as Sprite;
                if (value != null)
                    return value;
            }

            PropertyInfo property = type.GetProperty(candidateNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && typeof(Sprite).IsAssignableFrom(property.PropertyType))
            {
                Sprite value = property.GetValue(species) as Sprite;
                if (value != null)
                    return value;
            }
        }

        return null;
    }
}