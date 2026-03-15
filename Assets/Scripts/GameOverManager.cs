using System;
using UnityEngine;
public class GameOverManager : MonoBehaviour
{
    public void OnGameOver()
    {
        Debug.Log("¡GAME OVER! El jugador fue derrotado por la tormenta.");
        Time.timeScale = 0f; // Pausa el juego
    }
}